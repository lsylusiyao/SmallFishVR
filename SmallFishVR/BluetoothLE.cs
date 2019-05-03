using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace SmallFishVR
{
    /// <summary>
    /// 将蓝牙信息重新处理，变成ListView中易于显示的方式
    /// </summary>
    public class DeviceInformationDisplayBase : INotifyPropertyChanged
    {
        
        //更新图形界面
        public event PropertyChangedEventHandler PropertyChanged;
        protected internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string name = string.Empty;
        public string Name //设备名称
        {
            set
            {
                if(name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
                
            }
            get { return name; }
        }

        public string Id { set; get; } = string.Empty; //设备Id，用在给connect传值

        private string address = string.Empty;
        public string Address //设备地址
        {
            set
            {
                if (address != value)
                {
                    address = value;
                    OnPropertyChanged(nameof(Address));
                }
            }
            get { return address; }
        }

        private bool isConnected = false;
        public bool IsConnected //是否连接了
        {
            set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    OnPropertyChanged(nameof(IsConnected));
                }
            }
            get { return isConnected; }
        }

        private bool isConnectable = false;
        public bool IsConnectable //是否可连接，BLE特性
        {
            set
            {
                if (isConnectable != value)
                {
                    isConnectable = value;
                    OnPropertyChanged(nameof(IsConnectable));
                }
                
            }
            get { return isConnectable; }
        }

    }

    /// <summary>
    /// 使用新建列表的方式更新，因此需要一个中间类提供notify
    /// </summary>
    public class DeviceInformationDisplay : INotifyPropertyChanged
    {
        //更新图形界面
        public event PropertyChangedEventHandler PropertyChanged;
        protected internal virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 初始化列表，只是随便给一个~
        /// </summary>
        private List<DeviceInformationDisplayBase> devices
            = new List<DeviceInformationDisplayBase> { new DeviceInformationDisplayBase()
                { Name = "NameTest" , Address = "AddressTest", IsConnectable = true , IsConnected = true } }; //初始化
        public List<DeviceInformationDisplayBase> Devices
        {
            private set
            {
                if(devices != value)
                {
                    devices = value;
                    OnPropertyChanged(nameof(Devices));
                }
               
            }
            get { return devices; }
        }
        
        /// <summary>
        /// 刷新显示数据，使用新建整个list的方式
        /// </summary>
        /// <param name="ld">一个原有的list</param>
        public void Renew(List<DeviceInformation> ld)
        {
            List<DeviceInformationDisplayBase> ldnew = new List<DeviceInformationDisplayBase>(ld.Count); //新建空列表，初始化空间
            for (int i = 0; i < ld.Count; i++)
            {
                //使用key查找值
                if (!ld[i].Properties.TryGetValue("System.Devices.Aep.DeviceAddress", out object temp)) throw new Exception("竟然没找到？");
                string address = temp as string;
                if (!ld[i].Properties.TryGetValue("System.Devices.Aep.IsConnected", out temp)) throw new Exception("竟然没找到？");
                bool isConnected = Convert.ToBoolean(temp);
                if (!ld[i].Properties.TryGetValue("System.Devices.Aep.Bluetooth.Le.IsConnectable", out temp)) throw new Exception("竟然没找到？");
                bool isConnectable = Convert.ToBoolean(temp);

                //将新列表元素加入进去
                ldnew.Add(new DeviceInformationDisplayBase
                {
                    Name = ld[i].Name,
                    Id = ld[i].Id,
                    Address = address,
                    IsConnected = isConnected,
                    IsConnectable = isConnectable
                });
            }
            //重建列表
            Devices = new List<DeviceInformationDisplayBase>(ldnew);

        }
    }

    /// <summary>
    /// 低功耗蓝牙通信主要类
    /// </summary>
    public class BluetoothLE
    {
        private DeviceWatcher deviceWatcher; //查看device的

        private List<DeviceInformation> Devices { set; get; } = new List<DeviceInformation>(); //存储设备信息的

        public DeviceInformationDisplay DevicesDisplay { private set; get; } = new DeviceInformationDisplay(); //显示设备信息的


        //传输数据和访问服务等需要的GUID
        private Guid serviceGuid;
        private Guid writeGuid;
        private Guid readGuid;

        //设备的服务
        GattDeviceService deviceService;

        //使用Guid产生的CharacteristicsResult
        GattCharacteristic readCharacteristic;
        GattCharacteristic writeCharacteristic;

        //声明更新图形界面的委托和事件
        public delegate void UpdateBoxEventHandler(string msg);
        public UpdateBoxEventHandler UpdateBoxEvent;

        //委托传递函数
        private void Update(string msg) => UpdateBoxEvent?.Invoke(msg);

        //蓝牙设备
        private BluetoothLEDevice bluetoothLeDevice = null;

        //错误码
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)

        public BluetoothLE(string serviceGuid, string writeGuid, string readGuid)
        {
            try //尝试转换Guid
            {
                this.serviceGuid = new Guid(serviceGuid);
                this.writeGuid = new Guid(writeGuid);
                this.readGuid = new Guid(readGuid);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "GUID转换错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            StopBleDeviceWatcher();
        }

        /// <summary>
        /// 停止所有动作
        /// </summary>
        public async void StopAll()
        {
            try
            {
                StopBleDeviceWatcher();
                bool temp = await ClearBluetoothLEDeviceAsync();
            }
            catch (Exception e) when (e.GetType() == typeof(ObjectDisposedException)) { }
            
        }

        #region 监听部分

        /// <summary>
        /// 开始监听周围的配没配对的所有设备
        /// </summary>
        public void StartBleDeviceWatcher()
        {
            // 其他特性请见下网址
            // 网址是： https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            // BT_Code: 在一个队列中显示所有设备
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

            deviceWatcher =
                    DeviceInformation.CreateWatcher(
                        aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);

            // 将事件加入委托
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Stopped += DeviceWatcher_Stopped;

            // 清空列表
            Devices.Clear();

            // 开始
            deviceWatcher.Start();
        }

        /// <summary>
        /// 停止监听周围设备
        /// </summary>
        public void StopBleDeviceWatcher()
        {
            if (deviceWatcher != null)
            {
                // 删除委托方法
                deviceWatcher.Added -= DeviceWatcher_Added;
                deviceWatcher.Updated -= DeviceWatcher_Updated;
                deviceWatcher.Removed -= DeviceWatcher_Removed;
                deviceWatcher.Stopped -= DeviceWatcher_Stopped;

                // 停止监控
                deviceWatcher.Stop();
                deviceWatcher = null;
            }
        }

        /// <summary>
        /// 寻找列表中是否有此项
        /// </summary>
        /// <param name="id">设备的Id</param>
        /// <returns></returns>
        private DeviceInformation FindBluetoothLEDeviceDisplay(string id)
        {
            foreach (var bleDeviceDisplay in Devices)
            {
                if (bleDeviceDisplay.Id == id)
                {
                    return bleDeviceDisplay;
                }
            }
            return null;
        }

        
        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            await Task.Run(() => 
            {
                lock (this)
                {
                    Console.WriteLine(string.Format("Added {0}, {1}", deviceInfo.Id, deviceInfo.Name));

                    // 防止在程序结束之后依旧运行
                    if (sender == deviceWatcher)
                    {
                        // 确保device没有重复添加
                        if (FindBluetoothLEDeviceDisplay(deviceInfo.Id) == null)
                        {
                            Devices.Add(deviceInfo);
                            DevicesDisplay.Renew(Devices);
                        }
                    }
                }
            });
            
        }

        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfo)
        {
            await Task.Run(() =>
            {
                lock (this)
                {
                    Console.WriteLine(string.Format("Updated {0}{1}", deviceInfo.Id, ""));

                    // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                    if (sender == deviceWatcher)
                    {
                        // 更新
                        var blueDisp = FindBluetoothLEDeviceDisplay(deviceInfo.Id);
                        if (blueDisp != null)
                        {
                            blueDisp.Update(deviceInfo);
                            DevicesDisplay.Renew(Devices);
                        }

                    }
                }
            });
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            if(sender.Status != DeviceWatcherStatus.Aborted)
            {
                MessageBox.Show("停止错误！", "停止信息", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfo)
        {
            await Task.Run(() =>
            {
                lock (this)
                {
                    Console.WriteLine(string.Format("Removed {0}{1}", deviceInfo.Id, ""));

                    // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                    if (sender == deviceWatcher)
                    {
                        // 更新
                        var blueDisp = FindBluetoothLEDeviceDisplay(deviceInfo.Id);
                        if (blueDisp != null)
                        {
                            Devices.Remove(blueDisp);
                            DevicesDisplay.Renew(Devices);
                        }

                    }
                }
            });
        }

        #endregion
        // TODO：配对部分直接手动完成，不在这里写了

        #region 连接和收发数据部分

        /// <summary>
        /// 清空当前连接状态和监听状态
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ClearBluetoothLEDeviceAsync()
        {
            // Need to clear the CCCD from the remote device so we stop receiving notifications
            if(readCharacteristic != null)
            {
                var result = await readCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                if (result != GattCommunicationStatus.Success)
                {
                    return false;
                }
                else
                {
                    // readCharacteristic.ValueChanged -= ReadCharacteristic_ValueChanged;
                }
            }
            writeCharacteristic?.Service?.Dispose();
            readCharacteristic?.Service?.Dispose();
            deviceService?.Dispose();

            bluetoothLeDevice?.Dispose();
            bluetoothLeDevice = null;
            return true;
        }

        /// <summary>
        /// 连接函数，要使用await等待返回
        /// </summary>
        /// <param name="deviceItem">ListView中选择的Item</param>
        /// <returns></returns>
        public async Task<bool> Connect(object deviceItem)
        {
            if (!await ClearBluetoothLEDeviceAsync())
            {
                Update("重置状态错误，请重试");
                return false;
            }

            try
            {
                // BT_Code: BluetoothLEDevice.FromIdAsync must be called from a UI thread because it may prompt for consent.
                var deviceID = deviceItem as DeviceInformationDisplayBase;
                bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceID.Id);

                if (bluetoothLeDevice == null)
                {
                    Update("连接设备失败");
                }
            }
            catch (Exception ex) when (ex.HResult == E_DEVICE_NOT_AVAILABLE)
            {
                Update("蓝牙设备未开启");
                return false;
            }
            catch(Exception ex) when(ex.GetType() == typeof(NullReferenceException))
            {
                Update("没有成功选择列表里的选项");
                return false;
            }

            if (bluetoothLeDevice != null)
            {
                // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
                // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
                GattDeviceServicesResult deviceServicesResult = await bluetoothLeDevice.GetGattServicesForUuidAsync(serviceGuid);
                if (deviceServicesResult.Status == GattCommunicationStatus.Success)
                {
                    //服务处理
                    deviceService = deviceServicesResult.Services[0];
                    Update($"对于选定设备，找到了 {deviceServicesResult.Services.Count} 个服务。");

                    //读特征处理
                    GattCharacteristicsResult readCharacteristicsResult = await deviceService.GetCharacteristicsForUuidAsync(readGuid);
                    if (readCharacteristicsResult.Status == GattCommunicationStatus.Success)
                    {
                        readCharacteristic = readCharacteristicsResult.Characteristics[0];
                        Update($"对于选定设备，指定的GUID，找到了读特征。");
                        //订阅通知
                        // TODO: 这里暂时没有使用，因为string有bug，没想好怎么解决
                        // readCharacteristic.ValueChanged += ReadCharacteristic_ValueChanged;
                    }
                    else
                    { Update("读特征查询失败"); return false; }

                    //写特征处理
                    GattCharacteristicsResult writeCharacteristicsResult = await deviceService.GetCharacteristicsForUuidAsync(writeGuid);
                    if (writeCharacteristicsResult.Status == GattCommunicationStatus.Success)
                    {
                        writeCharacteristic = writeCharacteristicsResult.Characteristics[0];
                        Update($"对于选定设备，指定的GUID，找到了写特征。");
                    }
                    else
                    { Update("写特征查询失败"); return false; }
                }
                else
                {
                    Update("未找到任何服务");
                    return false;
                }
                return true;
            }
            else
            {
                MessageBox.Show("设备为null，请检查！", "对象错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void ReadCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // TODO: 这里暂时没有使用，因为string有bug，没想好怎么解决
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            //读取到的字符串提交给图形界面
            Update(reader.ReadString(args.CharacteristicValue.Length));
        }

        /// <summary>
        /// 将byte数组写入设备
        /// </summary>
        /// <param name="data"></param>
        public async void WriteCharacteristic(byte[] data)
        {
            var writer = new DataWriter();
            writer.WriteBytes(data);

            var result = await writeCharacteristic.WriteValueWithResultAsync(writer.DetachBuffer());
            if(result.Status != GattCommunicationStatus.Success)
            {
                MessageBox.Show("写入数据失败，请重试", "蓝牙通信写入", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }
        
        /// <summary>
        /// 为了方便对接原程序而做的封装
        /// </summary>
        /// <param name="data">写入的byte[]数据</param>
        /// <param name="offset">偏移（实际没用上）</param>
        /// <param name="count">数据长度，一定要正确</param>
        public void Write(byte[] data, int offset, int count)
        {
            if(count != data.Length)
            {
                MessageBox.Show("count数据输入错误","程序错误",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }
            WriteCharacteristic(data);
        }

        #endregion
    }
}
