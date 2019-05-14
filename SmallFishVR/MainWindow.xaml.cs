using BridgeDll;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace SmallFishVR
{
    
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region 定义和辅助功能区
        private BridgeClass bridge; //新建一个VR类（从CLR）
        DataStore data; //新建数据存储对象
        SendData2Fish BLESend = new SendData2Fish(
            serviceGuid: "0000fff0-0000-1000-8000-00805f9b34fb",
                writeGuid: "0000fff1-0000-1000-8000-00805f9b34fb",
                readGuid: "0000fff4-0000-1000-8000-00805f9b34fb"); //新建继承的带处理数据的蓝牙连接对象
        public string TempStr { get; set; } //委托临时使用的，提交给界面更新数据
        public bool IsVRSave2FileChecked { get; set; } = false; //是否把VR数据存储成文件
        public bool IsLeftHandFishChecked { get; set; } = false; //是否开启左手柄控制鱼
        public bool IsRightHandFishChecked { get; set; } = true; //是否开启右手柄控制鱼

        private bool isVRControlStart = false; //VR控制是否开启
        string isControlling = string.Empty; //是否在收集的数据前面加入文字

        Thread VRThread; //VR线程
        Thread listenVRThread; //监听VR数据线程，在有信息传来的时候也会报错
        Thread VRControlFishThread; //VR控制鱼的运动方向的控制
        Thread runFishThread; //按住按键时启动的运动线程

        public double[] LeftHandData { set; get; } = new double[8]; //左手显示数据（和零点的偏移）
        public double[] RightHandData { set; get; } = new double[8]; //右手显示数据（和零点的偏移）
        public double[] HandData { set; get; } = null; //真正用的时候的数据（和零点的偏移）
        public double[] HMDData { set; get; } = new double[6]; //头盔显示数据（和零点的偏移）
        public double[] TriggerData { set; get; } = new double[2]; //触发器的真实数据（不需要零点）

        public double GoCircleTime { set; get; } = 5000;
        public double[] GoSTime { set; get; } = new double[3] { 3000, 3000 ,3000};
        public bool IsLeft { set; get; } = true;
        public bool IsFirstRight { set; get; } = true;

        /// <summary>
        /// 委托更新SerialPort数据的图形界面
        /// </summary>
        /// <param name="msg">要添加到RichTextBox中的信息</param>
        private void UpdateBLEBox(string msg) => Dispatcher.Invoke((ThreadStart)delegate ()
        {
            BLEDataBox.AppendText(string.Format("{0}\r\n", msg));
        });

        /// <summary>
        /// 委托更新VR返回数据的图形界面
        /// </summary>
        /// <param name="msg">要添加到RichTextBox中的信息</param>
        private void UpdateVRBox(string msg) => Dispatcher.Invoke((ThreadStart)delegate () 
        {
            VRDataBox.AppendText(string.Format("{0}\r\n", msg));
        });

        /// <summary>
        /// 发送event，提醒数据已经改变
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region 窗口主要功能区

        public MainWindow()
        {
            InitializeComponent();
            data = new DataStore();
            BLESend.UpdateBoxEvent += UpdateBLEBox;
            Bindings();
            DefaultSettings();
        }

        /// <summary>
        /// 数据绑定类，所有的数据绑定类都写在这里
        /// </summary>
        void Bindings()
        {
            VRGrid.DataContext = this;
            VRSave2FileCheckBox.DataContext = this;
            rightHandFishCheckBox.DataContext = this;
            BLEDevicesListView.DataContext = BLESend.DevicesDisplay;
            goCircleTimeBox.DataContext = this;
            goSTimeBox0.DataContext = this;
            goSTimeBox1.DataContext = this;
            goSTimeBox2.DataContext = this;
            leftCheckBox.DataContext = this;
            rightFirstCheckBox.DataContext = this;
        }

        /// <summary>
        /// 缺省设置，暂时写在这里
        /// </summary>
        void DefaultSettings()
        {
            TempStr = string.Empty;
            
        }

        /// <summary>
        /// 重写关闭事件，关闭的时候自动关串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (VRThread != null && VRThread.IsAlive) VRThread.Abort();
            if (listenVRThread != null && listenVRThread.IsAlive) listenVRThread.Abort();
            if (VRControlFishThread != null && VRControlFishThread.IsAlive) VRControlFishThread.Abort();
            DirectoryInfo dI = new DirectoryInfo("../../data/"); //删除所有空的数据txt，省得考虑各种是否创建的问题了
            try
            {
                foreach (FileInfo file in dI.GetFiles("VRDataOn*.txt"))
                    if (file.Length == 0) file.Delete();
                BLESend.StopAll();
            }
            catch (Exception) { }
            
        }

        
        /// <summary>
        /// 手动创建一个关闭按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButton_Click(object sender, RoutedEventArgs e) => Close();

        /// <summary>
        /// 清空BLEDataBox VRDataBox的内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            BLEDataBox.Document.Blocks.Clear();
            VRDataBox.Document.Blocks.Clear();
        }

        #endregion

        #region 蓝牙相关功能区

        private void ListenBLEButton_Click(object sender, RoutedEventArgs e)
        {
            if (listenBLEButton.Content as string == "开始监听蓝牙")
            {
                var choice = MessageBox.Show($"配对过程需要手动完成，pin为0000，如未配对请手动配对。" +
                    $"{Environment.NewLine} \"是\"将会继续，\"否\"会返回。", "提醒", MessageBoxButton.YesNo);
                if (choice == MessageBoxResult.No) return;
                connectFishButton.IsEnabled = true;
                BLEDevicesListView.Visibility = Visibility.Visible;
                BLEDataBox.Visibility = Visibility.Hidden;
                listenBLEButton.Content = "停止监听蓝牙";
                BLESend.StartBleDeviceWatcher();
            }
            else
            {
                listenBLEButton.Content = "开始监听蓝牙";
                BLEDevicesListView.Visibility = Visibility.Hidden;
                BLEDataBox.Visibility = Visibility.Visible;
                connectFishButton.IsEnabled = false;
                BLESend.StopBleDeviceWatcher();
            }
            
        }

        /// <summary>
        /// 自动调节滚动位置到末尾
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BLEDataBox_TextChanged(object sender, TextChangedEventArgs e) => BLEDataBox.ScrollToEnd();
        #endregion
        
        #region VR功能区，包括VR控制机器鱼的线程

        /// <summary>
        /// 初始化VR的按钮，一旦没有连接成功硬件和Steam的话，程序会自动退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InitVRButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("即将初始化VR，请确认VR硬件已经连接，Steam已经打开。否则程序会自动退出" +
                "\n选择\"是\"来继续，\"否\"来返回", "VR连接提示", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bridge = new BridgeClass();
                    VRThread = new Thread(bridge.Run);
                    listenVRThread = new Thread(ListenVRThread);
                    startStopVRButton.IsEnabled = true;
                    showVRDevicesButton.IsEnabled = true;
                    setDataZeroButton.IsEnabled = true;
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Error:" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

            }
            else if (result == MessageBoxResult.No) return;
        }

        /// <summary>
        /// 查看VR设备列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowVRDevicesButton_Click(object sender, RoutedEventArgs e)
            => MessageBox.Show(bridge.GetDevices(), "VR设备列表");

        /// <summary>
        /// 开启关闭VR的按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartStopVRButton_Click(object sender, RoutedEventArgs e)
        {
            if (!bridge.GetKeepVRWorking())
            {
                VRThread.Start();
                listenVRThread.Start();
                startStopVRButton.Content = "停止监听VR";
                VRStateText.Text = "已连接";
                startStopVRControlButton.IsEnabled = true;
                setDataZeroButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); //相当于手动点一下置零
                MessageBox.Show("监听VR已经开始", "提示");
            }
            else
            {
                bridge.SetKeepVRWorking(false);
                Thread.Sleep(20);
                bridge.Dispose();
                startStopVRButton.IsEnabled = false;
                showVRDevicesButton.IsEnabled = false;
                setDataZeroButton.IsEnabled = false;
                startStopVRButton.Content = "开始监听VR";
                VRStateText.Text = "未连接";
                MessageBox.Show("监听VR已经停止", "提示");
            }
        }

        /// <summary>
        /// 监听VR后台的返回数据的线程，并通过Binding把数据给图形界面
        /// </summary>
        private void ListenVRThread()
        {
            FileStream fs = new FileStream($"../../data/VRDataOn{DateTime.Now.ToString("yyyy-MM-dd_hh_mm")}.txt", 
                FileMode.Append, FileAccess.Write);
            StreamWriter w = new StreamWriter(fs);
            while (VRThread.IsAlive)
            {
                data.HMDDataOrigin = bridge.GetHMD(); //各种拿到数据
                data.LeftHandDataOrigin = bridge.GetLeftHand();
                data.RightHandDataOrigin = bridge.GetRightHand();
                //TriggerData = bridge.GetTrigger();
                for (int i = 0; i < LeftHandData.Length; i++)
                {
                    if (i < 6) HMDData[i] = data.HMDDataOrigin[i] - data.HMDZero[i]; //与零点相减，获得偏差量
                    LeftHandData[i] = data.LeftHandDataOrigin[i] - data.LeftHandZero[i];
                    RightHandData[i] = data.RightHandDataOrigin[i] - data.RightHandZero[i];
                }
                NotifyPropertyChanged(nameof(HMDData)); //更新图形界面用
                NotifyPropertyChanged(nameof(LeftHandData));
                NotifyPropertyChanged(nameof(RightHandData));
                //NotifyPropertyChanged(nameof(TriggerData));
                Thread.Sleep(10);
                if (IsVRSave2FileChecked) //存储数据
                {
                    w.WriteLine($"{isControlling}HMD:"); //如果是在控制状态，就加入Controlling这个字符串，方便后续数据处理
                    foreach (var a in HMDData)
                    {
                        w.Write($"{a}, ");
                    }
                    w.WriteLine($"\n{isControlling}LeftHand:");
                    foreach (var a in LeftHandData)
                    {
                        w.Write($"{a}, ");
                    }
                    w.WriteLine($"\n{isControlling}RightHand:");
                    foreach (var a in RightHandData)
                    {
                        w.Write($"{a}, ");
                    }
                    w.WriteLine("\n");
                    
                }
                
                if (bridge.GetIsStrGiven())
                {
                    UpdateVRBox(bridge.GetInfoStr());
                    lock (this)
                    {
                        bridge.ClearInfoStr();
                        bridge.SetIsStrGiven(false);
                    }
                }
                Thread.Sleep(15);
            }
            w.Close();
        }

        /// <summary>
        /// 设置数据零点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetDataZeroButton_Click(object sender, RoutedEventArgs e)
        {
            //在data中已经new了一个区域，因此直接赋值就行
            data.HMDDataOrigin.CopyTo(data.HMDZero, 0);
            data.LeftHandDataOrigin.CopyTo(data.LeftHandZero, 0);
            data.RightHandDataOrigin.CopyTo(data.RightHandZero, 0);
        }

        /// <summary>
        /// 开启VR控制机器鱼的线程（或者终止）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartStopVRControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isVRControlStart)
            {
                isControlling = "Controlling";
                VRControlFishThread = new Thread(VRControlFishThreadFunc);
                VRControlFishThread.Start();
                isVRControlStart = true;
                startStopVRControlButton.Content = "停止VR控制";
                VRControlStateBox.Text = "VR已控制";
                InitVRButton.IsEnabled = false;
                MessageBox.Show("VR控制机器鱼已经开始，手动控制仍然生效", "提示");
            }
            else
            {
                isControlling = string.Empty;
                VRControlFishThread.Abort();
                isVRControlStart = false;
                startStopVRControlButton.Content = "开始VR控制";
                VRControlStateBox.Text = "VR未控制";
                InitVRButton.IsEnabled = true;
                MessageBox.Show("VR控制机器鱼已经停止", "提示");
            }

        }
        
        /// <summary>
        /// 将VR数据框滚动到底部
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VRDataBox_TextChanged(object sender, TextChangedEventArgs e) 
            => VRDataBox.ScrollToEnd();

        /// <summary>
        /// VR控制机器鱼的后台数据解析和发送指令的线程
        /// </summary>
        private void VRControlFishThreadFunc()
        {
            /*
            * Also:
            * Open VR Convention (same as OpenGL)
            * right-handed system
            * +y is up，逆时针切换为正(-)，顺时针为负(+)
            * +x is to the right，手柄向后为正，向前为负
            * -z is going away from you ，左转为正，右转为负
            */
           
            int[] divisionPoint = new int[] { 15, 25, 40, 55 }; //Stop-1-2-3-4的分界角度点
            // bool isChangedColor = false; //颜色改变完了的话，就不重复发送了，直到手柄恢复到0位置
            bool keepWhileFlag = true; //保持循环控制，在停止的时候变成false来直接结束循环


            const int COLOR = 3; //数据正常
            const int SPEED = 5; //速度分析使用
            const int LR = 6; //左右
            const int FB = 7; //前后
            SendData2Fish.Speed tempSpeed = SendData2Fish.Speed.Low; //由前后角度出来的速度

            while (keepWhileFlag)
            {
                Thread.Sleep(250);
                
                //对于两个机器鱼的适配，不想改了
                for (int i = 0; i < 2; i++)
                {
                    if (!IsLeftHandFishChecked && !IsRightHandFishChecked)
                    {
                        MessageBox.Show("没有选择手柄控制，请至少选择一个", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        keepWhileFlag = false;
                        break;
                    }
                    if (i == 0)
                    {
                        if (IsLeftHandFishChecked) HandData = LeftHandData;
                        else continue;
                    }
                    if (i == 1)
                    {
                        if (IsRightHandFishChecked) HandData = RightHandData;
                        else continue;
                    }

                    #region 设置颜色部分

                    if (HandData[COLOR] >= divisionPoint[1] || HandData[COLOR] <= -divisionPoint[1])
                    {
                        BLESend.SetColorCycle(i, HandData[COLOR] > 0 ? '-' : '+');
                    }
                    // if(TriggerData[i] == 1) spSend.SetColorCycle(i, '+'); //trigger更新颜色方法，备用

                    #endregion

                    #region 设置速度，手柄向前为负
                    if (HandData[SPEED] >= -divisionPoint[0]) BLESend.SetMove(i, SendData2Fish.Direction.Stop);
                    else if (HandData[SPEED] >= -divisionPoint[1]) tempSpeed = SendData2Fish.Speed.VeryLow;
                    else if (HandData[SPEED] >= -divisionPoint[2]) tempSpeed = SendData2Fish.Speed.Low;
                    else if (HandData[SPEED] >= -divisionPoint[3]) tempSpeed = SendData2Fish.Speed.Medium;
                    else tempSpeed = SendData2Fish.Speed.High;
                    #endregion

                    #region 设置前后左右靠触摸板控制
                    if (Math.Abs(HandData[LR]) < 10 && Math.Abs(HandData[FB]) < 10) //停止
                        BLESend.SetMove(i, SendData2Fish.Direction.Stop);
                    else if(Math.Abs(HandData[LR]) >= Math.Abs(HandData[FB])) //控制左右转
                    {
                        if (HandData[LR] > 0) BLESend.SetMove(i, SendData2Fish.Direction.Right, tempSpeed);
                        else BLESend.SetMove(i, SendData2Fish.Direction.Left, tempSpeed);
                    }
                    else //控制向前运动
                    {
                        if (HandData[FB] > 0) BLESend.SetMove(i, SendData2Fish.Direction.Forward, tempSpeed);
                        else BLESend.SetMove(i, SendData2Fish.Direction.Stop);
                    }
                    #endregion

                    #region 原来的控制方法
                    //if (HandData[FB] < divisionPoint[0] && HandData[FB] > -divisionPoint[0] &&
                    //    HandData[LR] < divisionPoint[0] && HandData[LR] > -divisionPoint[0]) //在一开始的范围内就认为停止
                    //{
                    //    spSend.SetMove(i, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                    //    //isSent = false;
                    //    isStop = false;
                    //} //这里速度随便给
                    //else
                    //{
                    //    if (HandData[FB] > divisionPoint[0]) //左右任意，只要前后足够靠后，就认为停止
                    //    {
                    //        spSend.SetMove(i, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                    //        isStop = true;
                    //        continue;
                    //    }

                    //    #region 左右转发送数据

                    //    //只有左右方向恢复，鱼才会前进运动
                    //    if (HandData[LR] <= divisionPoint[1] || HandData[LR] >= -divisionPoint[1])
                    //    {
                    //        spSend.SetMove(i,
                    //          HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                    //          SendData2Fish.Speed.VeryLow, isStop);
                    //        //isSent = true;
                    //    }
                    //    else if (HandData[LR] <= divisionPoint[2] || HandData[LR] >= -divisionPoint[2])
                    //    {
                    //        spSend.SetMove(i,
                    //          HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                    //          SendData2Fish.Speed.Low, isStop);
                    //        continue;
                    //        //isSent = true;
                    //    }
                    //    else if (HandData[LR] <= divisionPoint[3] || HandData[LR] >= -divisionPoint[3])
                    //    {
                    //        spSend.SetMove(i,
                    //          HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                    //          SendData2Fish.Speed.Medium, isStop);
                    //        continue;
                    //        //isSent = true;
                    //    }
                    //    else
                    //    {
                    //        spSend.SetMove(i,
                    //          HandData[LR] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                    //          SendData2Fish.Speed.High, isStop);
                    //        continue;
                    //        //isSent = true;
                    //    }
                    //    #endregion

                    //    #region 前进发送数据

                    //    if (HandData[FB] > -divisionPoint[0] && HandData[FB] < 0)
                    //    {
                    //        //这条其实不写也行，反正应该到不了这个区域
                    //        spSend.SetMove(i, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                    //    }
                    //    else if (HandData[FB] > -divisionPoint[1])
                    //    { spSend.SetMove(i, SendData2Fish.Direction.Forward, SendData2Fish.Speed.Low); }
                    //    else if (HandData[FB] > -divisionPoint[2])
                    //    { spSend.SetMove(i, SendData2Fish.Direction.Forward, SendData2Fish.Speed.Medium); }
                    //    else { spSend.SetMove(i, SendData2Fish.Direction.Forward, SendData2Fish.Speed.High); }

                    //    #endregion
                    //}
                    #endregion
                }


            }
        }

        /// <summary>
        /// VR控制机器鱼的后台数据解析和发送指令的线程，使用检测绝对位置的办法
        /// </summary>
        private void VRControlFishThreadFunc2()
        {
            /*
            * Also:
            * Open VR Convention (same as OpenGL)
            * right-handed system
            * +y is up，逆时针切换为正(-)，顺时针为负(+)
            * +x is to the right，手柄向后为正，向前为负
            * -z is going away from you ，左转为正，右转为负
            * 数据单位是cm
            */

            int[] divisionPoint = new int[] { 8, 16, 24, 30 }; //Stop-1-2-3-4的分界距离点
            // bool isChangedColor = false; //颜色改变完了的话，就不重复发送了，直到手柄恢复到0位置
            bool keepWhileFlag = true; //保持循环控制，在停止的时候变成false来直接结束循环
            bool isStop = false; //已经停止的flag，用于发送数据


            const int COLOR1 = 3; //旋转控制方法
            const int COLOR2 = 1; //上下移动控制方法
            const int LR = 0; //左右
            const int FB = 2; //前后
            //SendData2Fish.Speed tempSpeed = SendData2Fish.Speed.Low; //由前后角度出来的速度

            while (keepWhileFlag)
            {
                Thread.Sleep(250); //这里不能太短了，测试发现VR的数据响应较慢，原因未知，再加上鱼本身反应速度不快，就这样吧

                //对于两个机器鱼的适配
                for (int i = 0; i < 2; i++)
                {
                    if (!IsLeftHandFishChecked && !IsRightHandFishChecked)
                    {
                        MessageBox.Show("没有选择手柄控制，请至少选择一个", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        keepWhileFlag = false;
                        break;
                    }
                    if (i == 0)
                    {
                        if (IsLeftHandFishChecked) HandData = LeftHandData;
                        else continue;
                    }
                    if (i == 1)
                    {
                        if (IsRightHandFishChecked) HandData = RightHandData;
                        else continue;
                    }

                    #region 设置颜色部分

                    if (HandData[COLOR1] >= 15 || HandData[COLOR1] <= -15)
                    {
                        BLESend.SetColorCycle(i, HandData[COLOR1] > 0 ? '-' : '+');
                    } //旋转控制

                    if(HandData[COLOR2] >= divisionPoint[0] || HandData[COLOR2] <= -divisionPoint[0])
                    {
                        BLESend.SetColorCycle(i,HandData[COLOR2] > 0 ? '-' : '+');
                    } //上下控制

                    #endregion

                    if (HandData[FB] < divisionPoint[0] && HandData[FB] > -divisionPoint[0] &&
                        HandData[LR] < divisionPoint[0] && HandData[LR] > -divisionPoint[0]) //在一开始的范围内就认为停止
                    {
                        BLESend.SetMove(i, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                        //isSent = false;
                        isStop = false;
                    } //这里速度随便给
                    else
                    {
                        if (HandData[FB] > -divisionPoint[0]) //左右任意，只要前后足够靠后，就认为停止
                        {
                            BLESend.SetMove(i, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                            isStop = true;
                            continue;
                        }

                        #region 左右转发送数据

                        //只有左右方向恢复，鱼才会前进运动
                        if (HandData[LR] <= divisionPoint[1] || HandData[LR] >= -divisionPoint[1])
                        {
                            BLESend.SetMove(i,
                              HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                              SendData2Fish.Speed.VeryLow, isStop);
                            //isSent = true;
                        }
                        else if (HandData[LR] <= divisionPoint[2] || HandData[LR] >= -divisionPoint[2])
                        {
                            BLESend.SetMove(i,
                              HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                              SendData2Fish.Speed.Low, isStop);
                            continue;
                            //isSent = true;
                        }
                        else if (HandData[LR] <= divisionPoint[3] || HandData[LR] >= -divisionPoint[3])
                        {
                            BLESend.SetMove(i,
                              HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                              SendData2Fish.Speed.Medium, isStop);
                            continue;
                            //isSent = true;
                        }
                        else
                        {
                            BLESend.SetMove(i,
                              HandData[LR] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                              SendData2Fish.Speed.High, isStop);
                            continue;
                            //isSent = true;
                        }
                        #endregion

                        #region 前进发送数据

                        if (HandData[FB] > -divisionPoint[0] && HandData[FB] < 0)
                        {
                            //这条其实不写也行，反正应该到不了这个区域
                            BLESend.SetMove(i, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                        }
                        else if (HandData[FB] > -divisionPoint[1])
                        { BLESend.SetMove(i, SendData2Fish.Direction.Forward, SendData2Fish.Speed.Low); }
                        else if (HandData[FB] > -divisionPoint[2])
                        { BLESend.SetMove(i, SendData2Fish.Direction.Forward, SendData2Fish.Speed.Medium); }
                        else { BLESend.SetMove(i, SendData2Fish.Direction.Forward, SendData2Fish.Speed.High); }

                        #endregion
                    }
                }


            }
        }
        #endregion

        #region 手动控制机器鱼功能区（右手柄）

        /// <summary>
        /// 连接鱼
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ConnectFishButton_Click(object sender, RoutedEventArgs e)
        {
            if (connectFishButton.Content as string == "连接鱼")
            {
                connectFishButton.IsEnabled = false;
                if (listenBLEButton.Content as string != "开始监听蓝牙")
                    listenBLEButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                if (!await BLESend.Connect(BLEDevicesListView.SelectedItem))
                {
                    MessageBox.Show("连接失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    connectFishButton.IsEnabled = true;
                    return;
                }
                else
                {
                    MessageBox.Show("连接成功");
                    connectFishButton.Content = "断开连接";

                }
                //更改文字，开启各种控件
                connectStateText.Text = "已连接";
                connectFishButton.IsEnabled = true;
                speedSlider.IsEnabled = true;
                switchColor1Button.IsEnabled = true;
                switchColor2Button.IsEnabled = true;
                turnForwardButton.IsEnabled = true;
                turnLeftButton.IsEnabled = true;
                turnRightButton.IsEnabled = true;
            }
            else
            {
                BLESend.StopAll();
                //更改文字，开启各种控件
                connectFishButton.Content = "连接鱼";
                connectStateText.Text = "未连接";
                speedSlider.IsEnabled = false;
                switchColor1Button.IsEnabled = false;
                switchColor2Button.IsEnabled = false;
                turnForwardButton.IsEnabled = false;
                turnLeftButton.IsEnabled = false;
                turnRightButton.IsEnabled = false;
            }

            
        }

        /// <summary>
        /// 逆时针方向（-）设置鱼颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchColor1Button_Click(object sender, RoutedEventArgs e) => BLESend.SetColorCycle(0, '-');

        /// <summary>
        /// 顺时针方向（+）设置鱼颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchColor2Button_Click(object sender, RoutedEventArgs e) => BLESend.SetColorCycle(0, '+');

        /// <summary>
        /// 左转按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnLeftButton_Click(object sender, RoutedEventArgs e) 
            => BLESend.SetMove(0, SendData2Fish.Direction.Left, (SendData2Fish.Speed)speedSlider.Value);

        /// <summary>
        /// 前进按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnForwardButton_Click(object sender, RoutedEventArgs e) 
            => BLESend.SetMove(0, SendData2Fish.Direction.Forward, (SendData2Fish.Speed)speedSlider.Value);

        /// <summary>
        /// 右转按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnRightButton_Click(object sender, RoutedEventArgs e) 
            => BLESend.SetMove(0, SendData2Fish.Direction.Right, (SendData2Fish.Speed)speedSlider.Value);

        /// <summary>
        /// 按下前进按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnForwardButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            runFishThread = new Thread(() => {
                while (true)
                {
                    Dispatcher.Invoke(() =>
                    { BLESend.SetMove(0, SendData2Fish.Direction.Forward, (SendData2Fish.Speed)speedSlider.Value); });
                    Thread.Sleep(100);
                }
            });
            runFishThread.Start();
        }

        /// <summary>
        /// 松开按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnForwardButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => runFishThread?.Abort();


        /// <summary>
        /// 按下左转按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnLeftButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            runFishThread = new Thread(() => {
                while (true)
                {
                    Dispatcher.Invoke(() =>
                    { BLESend.SetMove(0, SendData2Fish.Direction.Left, (SendData2Fish.Speed)speedSlider.Value); });
                    Thread.Sleep(100);
                }
            });
            runFishThread.Start();
        }

        /// <summary>
        /// 松开按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnLeftButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => runFishThread?.Abort();

        /// <summary>
        /// 按下右转按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnRightButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            runFishThread = new Thread(() => {
                while (true)
                {
                    Dispatcher.Invoke(() =>
                    { BLESend.SetMove(0, SendData2Fish.Direction.Right, (SendData2Fish.Speed)speedSlider.Value); });
                    Thread.Sleep(100);
                }
            });
            runFishThread.Start();
        }

        /// <summary>
        /// 松开按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnRightButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => runFishThread?.Abort();

        #endregion

        #region 尝试自动化功能区

        private void GoCircleThread()
        {
            var startTime = DateTime.Now;
            var endTime = DateTime.Now;
            endTime.AddMilliseconds(GoCircleTime);
            while(DateTime.Now <= endTime)
            {
                BLESend.SetMove(0, IsLeft ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right, SendData2Fish.Speed.Medium);
                Thread.Sleep(100);
            }
        }

        private void GoSThread()
        {
            var startTime = DateTime.Now;
            var seprateTime1 = DateTime.Now;
            seprateTime1.AddMilliseconds(GoSTime[0]);
            var seprateTime2 = seprateTime1; //copy
            seprateTime2.AddMilliseconds(GoSTime[1] + 1);
            var seprateTime3 = seprateTime1; //copy
            seprateTime3.AddMilliseconds(GoSTime[1] + GoSTime[2] + 2);

            while (DateTime.Now <= seprateTime1)
            {
                BLESend.SetMove(0, IsFirstRight ? SendData2Fish.Direction.Right : SendData2Fish.Direction.Left, SendData2Fish.Speed.Medium);
                Thread.Sleep(100);
            }
            while (DateTime.Now <= seprateTime2)
            {
                BLESend.SetMove(0, IsFirstRight ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right, SendData2Fish.Speed.Medium);
                Thread.Sleep(100);
            }
            while(DateTime.Now <= seprateTime3)
            {
                BLESend.SetMove(0, SendData2Fish.Direction.Forward, SendData2Fish.Speed.Medium);
                Thread.Sleep(100);
            }
        }

        private void GoCircleButton_Click(object sender, RoutedEventArgs e)
        {
            var a = new Thread(GoCircleThread);
            a.Start();
            MessageBox.Show("转圈圈开始");
        }

        private void GoSButton_Click(object sender, RoutedEventArgs e)
        {
            var a = new Thread(GoSThread);
            a.Start();
            MessageBox.Show("走S开始");
        }
    }
    #endregion
}
