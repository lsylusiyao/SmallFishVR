using BridgeDll;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
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
        #region 定义区
        private BridgeClass bridge; //新建一个VR类（从CLR）
        DataStore data; //新建数据存储对象
        SendData2Fish spSend = new SendData2Fish(); //新建继承的带处理数据的串口对象
        public string TempStr { get; set; } //委托临时使用的，提交给界面更新数据
        public bool IsVRSave2FileChecked { get; set; } = false; //是否把VR数据存储成文件
        public bool IsLeftHandFishChecked { get; set; } = true; //是否开启左手柄控制鱼
        public bool IsRightHandFishChecked { get; set; } = true; //是否开启右手柄控制鱼

        private bool isVRControlStart = false; //VR控制是否开启

        Thread listenSPDataThread; //监听串口数据的线程
        Thread VRThread; //VR线程
        Thread listenVRThread; //监听VR数据线程，在有信息传来的时候也会报错
        Thread VRControlFishThread; //VR控制鱼的运动方向的控制

        public double[] LeftHandData { set; get; } = new double[8];
        public double[] RightHandData { set; get; } = new double[8];
        public double[] HandData { set; get; } = null;
        public double[] HMDData { set; get; } = new double[6];

        /// <summary>
        /// 委托更新图形界面
        /// </summary>
        /// <param name="msg">要添加到RichTextBox中的信息</param>
        private void UpdateBox(string msg) 
        {
            Dispatcher.Invoke((ThreadStart)delegate () {
                SPDataBox.AppendText(string.Format("{0}\r\n", msg)); });
        }

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
            Bindings();
            DefaultSettings();
        }

        /// <summary>
        /// 数据绑定类，所有的数据绑定类都写在这里
        /// </summary>
        void Bindings()
        {
            setPortGrid.DataContext = data;
            VRGrid.DataContext = this;
            setIPPortGrid.DataContext = data;
            VRSave2FileCheckBox.DataContext = this;
            leftHandFishCheckBox.DataContext = this;
            rightHandFishCheckBox.DataContext = this;
        }

        /// <summary>
        /// 缺省设置，默认选择的Index，暂时写在这里
        /// </summary>
        void DefaultSettings()
        {
            baudRateBox.SelectedIndex = 3;
            parityBox.SelectedIndex = 0;
            dataBitsBox.SelectedIndex = 3;
            stopBitsBox.SelectedIndex = 0;
            TempStr = string.Empty;
            data.LeftHandFishIP = "192.168.4.2";
            data.LeftHandFishPort = 1001;
            data.RightHandFishIP = "192.168.4.3";
            data.RightHandFishPort = 1002;
        }

        /// <summary>
        /// 重写关闭事件，关闭的时候自动关串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (spSend.IsOpen) spSend.Close();
            if (listenSPDataThread != null && listenSPDataThread.IsAlive) listenSPDataThread.Abort();
            if (VRThread != null && VRThread.IsAlive) VRThread.Abort();
            if (listenVRThread != null && listenVRThread.IsAlive) listenVRThread.Abort();
            if (VRControlFishThread != null && VRControlFishThread.IsAlive) VRControlFishThread.Abort();
        }

        /// <summary>
        /// 将获取到的数据解析成为真正的数据，并准备存放到SerialPort对象中
        /// </summary>
        public void Switch2RealInfo()
        {
            data.Real.portName = data.SpList[portBox.SelectedIndex];
            data.Real.baudRate = data.BaudRate[baudRateBox.SelectedIndex];
            data.Real.dataBits = data.DataBits[dataBitsBox.SelectedIndex];
            switch (stopBitsBox.SelectedIndex)
            {
                case 0:
                    data.Real.stopBits = StopBits.One;
                    break;
                case 1:
                    data.Real.stopBits = StopBits.OnePointFive;
                    break;
                case 2:
                    data.Real.stopBits = StopBits.Two;
                    break;
                default:
                    MessageBox.Show("Error：参数不正确!", "Error");
                    break;
            }
            if (parityBox.SelectedIndex >= 0) { data.Real.parity = (Parity)parityBox.SelectedIndex; }
            else { MessageBox.Show("Error：参数不正确!", "Error"); }
        }

        /// <summary>
        /// 手动创建一个关闭按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButton_Click(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region 串口功能区
        /// <summary>
        /// 重新统计串口名称列表的按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReCountCOMButton_Click(object sender, RoutedEventArgs e)
        {
            data.SpList = new List<string>(SerialPort.GetPortNames());
            MessageBox.Show("重新检查完毕", "端口列表");
        }

        /// <summary>
        /// 切换开启关闭串口的按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenClosePortButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Switch2RealInfo();
                if (!spSend.IsOpen)
                {
                    Switch2RealInfo();
                    spSend.PortName = data.Real.portName;
                    spSend.BaudRate = data.Real.baudRate;
                    spSend.StopBits = data.Real.stopBits;
                    spSend.DataBits = data.Real.dataBits;
                    spSend.Parity = data.Real.parity;
                    spSend.DtrEnable = true;
                    spSend.RtsEnable = true;
                    spSend.ReadTimeout = 1000; //miliseconds

                    spSend.Close(); //为了防止之前端口打开，因此先关闭

                    spSend.DataReceived += new SerialDataReceivedEventHandler(Sp_DataReceived); //收到数据的时候激活这个事件
                    
                    listenSPDataThread = new Thread(()=> //一个线程，用来将数据实时发送给委托的
                    {
                        while(true)
                        {
                            if (TempStr.Length > 0)
                            {
                                UpdateBox(TempStr);
                                lock (this) {TempStr = string.Empty; }
                            }
                            Thread.Sleep(15);
                        }
                    });
                    spSend.Open();
                    listenSPDataThread.Start();
                    listenSPDataThread.IsBackground = true;
                    openClosePortButton.Content = "关闭端口"; //更改显示文字
                    portStateText.Text = "已打开"; //更改下方文字
                    connectFishButton.IsEnabled = true;
                    connectFishButton2.IsEnabled = true;
                    checkStateButton.IsEnabled = true;
                    
                }
                else
                {
                    spSend.Close();
                    listenSPDataThread.Abort();
                    openClosePortButton.Content = "打开端口";
                    portStateText.Text = "未打开";
                    connectFishButton.IsEnabled = false;
                    connectFishButton2.IsEnabled = false;
                    checkStateButton.IsEnabled = false;
                    turnForwardButton.IsEnabled = false;
                    turnLeftButton.IsEnabled = false;
                    turnRightButton.IsEnabled = false;
                    switchColor1Button.IsEnabled = false;
                    switchColor2Button.IsEnabled = false;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Error",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }

        }
        /// <summary>
        /// 接收到数据时的事件，将文本保存到临时string中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                lock (this)
                {
                    TempStr = spSend.ReadLine();
                    //spSend.DiscardInBuffer();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "出错提示",MessageBoxButton.OK, MessageBoxImage.Error);
            }
            

        }

        /// <summary>
        /// 检查连接IP和状态的，由人看返回值决定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckStateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                spSend.SetInit(SendData2Fish.Function.GetIPs);
                Thread.Sleep(100);
                spSend.SetInit(SendData2Fish.Function.CheckStatus);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /// <summary>
        /// 清空SPDataBox的内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SPDataBox.Document.Blocks.Clear();
        }

        /// <summary>
        /// 自动调节滚动位置到末尾
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SPDataBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SPDataBox.ScrollToEnd();
        }
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
            
            while (VRThread.IsAlive)
            {
                data.HMDDataOrigin = bridge.GetHMD();
                data.LeftHandDataOrigin = bridge.GetLeftHand();
                data.RightHandDataOrigin = bridge.GetRightHand();
                for (int i = 0; i < LeftHandData.Length; i++)
                {
                    if (i < 6) HMDData[i] = data.HMDDataOrigin[i] - data.HMDZero[i];
                    LeftHandData[i] = data.LeftHandDataOrigin[i] - data.LeftHandZero[i];
                    RightHandData[i] = data.RightHandDataOrigin[i] - data.RightHandZero[i];
                }
                NotifyPropertyChanged(nameof(HMDData));
                NotifyPropertyChanged(nameof(LeftHandData));
                NotifyPropertyChanged(nameof(RightHandData));
                Thread.Sleep(100);
                if (IsVRSave2FileChecked)
                {
                    FileStream fs = new FileStream("../VRData.txt", FileMode.Append, FileAccess.Write);
                    StreamWriter w = new StreamWriter(fs);
                    w.WriteLine("HMD: ");
                    foreach (var a in HMDData)
                    {
                        w.Write(a.ToString() + ", ");
                    }
                    w.WriteLine("\nLeftHand: ");
                    foreach (var a in LeftHandData)
                    {
                        w.Write(a.ToString() + ", ");
                    }
                    w.WriteLine("\nRightHand: ");
                    foreach (var a in RightHandData)
                    {
                        w.Write(a.ToString() + ", ");
                    }
                    w.WriteLine("\n");
                    w.Close();
                }
                

                if (bridge.GetIsStrGiven())
                    {
                        MessageBox.Show(bridge.GetInfoStr(), "VR信息");
                        lock (this)
                        {
                            bridge.ClearInfoStr();
                            bridge.SetIsStrGiven(false);
                        }
                    }
                Thread.Sleep(15);
            }
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
                VRControlFishThread.Abort();
                isVRControlStart = false;
                startStopVRControlButton.Content = "开始VR控制";
                VRControlStateBox.Text = "VR未控制";
                InitVRButton.IsEnabled = true;
                MessageBox.Show("VR控制机器鱼已经停止", "提示");
            }

        }

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
            /*
             * 数组解析：
             * 0 ~ 2： 位置的x， y, z坐标
             * 3 ~ 5： 欧拉角x, y, z的度数（角度）
             * 6 ~ 7： 手柄的状态的x, y
             */
            int[] divisionPoint = new int[] { 15, 25, 40, 50 }; //Stop-1-2-3-4的分界角度点
            bool isStop = false; //鱼在停止范围内，所有数据都不发送
            //bool isSent = false;
            bool isChangedColor = false; //颜色改变完了的话，就不重复发送了，直到手柄恢复到0位置
            bool keepWhileFlag = true; //颜色改变完了的话，就不重复发送了，直到手柄恢复到0位置
            while (keepWhileFlag)
            {
                Thread.Sleep(800); //这里不能太短了，测试发现VR的数据响应是1Hz，估计是电脑性能不够？？再加上鱼本身反应速度不快，就这样吧
                
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
                    Thread.Sleep(50);
                    if (i == 1)
                    {
                        if (IsRightHandFishChecked) HandData = RightHandData;
                        else continue;
                    }

                    #region 设置颜色部分

                    if (HandData[4] < divisionPoint[0] && HandData[4] > -divisionPoint[0]) { isChangedColor = false; }
                    else
                    {
                        spSend.SetColorCycle(i, HandData[4] > 0 ? '-' : '+', isChangedColor);
                        isChangedColor = true; //每次转手柄，颜色只改变一次，直到恢复到初始位置
                    }
                    #endregion

                    if (HandData[3] < divisionPoint[0] && HandData[3] > -divisionPoint[0] &&
                        HandData[5] < divisionPoint[0] && HandData[5] > -divisionPoint[0]) //在一开始的范围内就认为停止
                    {
                        spSend.SetMove(i, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                        //isSent = false;
                        isStop = false;
                    } //这里速度随便给
                    else
                    {
                        if (HandData[3] > divisionPoint[0]) //左右任意，只要前后足够靠后，就认为停止
                        {
                            spSend.SetMove(i, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                            isStop = true;
                            continue;
                        }

                        #region 左右转发送数据

                        //只有左右方向恢复，鱼才会前进运动
                        if (HandData[5] <= divisionPoint[1] || HandData[5] >= -divisionPoint[1])
                        {
                            spSend.SetMove(i,
                              HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                              SendData2Fish.Speed.VeryLow, isStop);
                            //isSent = true;
                        }
                        else if (HandData[5] <= divisionPoint[2] || HandData[5] >= -divisionPoint[2])
                        {
                            spSend.SetMove(i,
                              HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                              SendData2Fish.Speed.Low, isStop);
                            continue;
                            //isSent = true;
                        }
                        else if (HandData[5] <= divisionPoint[3] || HandData[5] >= -divisionPoint[3])
                        {
                            spSend.SetMove(i,
                              HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                              SendData2Fish.Speed.Medium, isStop);
                            continue;
                            //isSent = true;
                        }
                        else
                        {
                            spSend.SetMove(i,
                              HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                              SendData2Fish.Speed.High, isStop);
                            continue;
                            //isSent = true;
                        }
                        #endregion

                        #region 前进发送数据

                        if (HandData[3] > -divisionPoint[0] && HandData[3] < 0)
                        {
                            //这条其实不写也行，反正应该到不了这个区域
                            spSend.SetMove(i, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                        }
                        else if (HandData[3] > -divisionPoint[1])
                        { spSend.SetMove(i, SendData2Fish.Direction.Forward, SendData2Fish.Speed.Low); }
                        else if (HandData[3] > -divisionPoint[2])
                        { spSend.SetMove(i, SendData2Fish.Direction.Forward, SendData2Fish.Speed.Medium); }
                        else { spSend.SetMove(i, SendData2Fish.Direction.Forward, SendData2Fish.Speed.High); }

                        #endregion
                    }
                }

                
            }
        }
        #endregion

        #region 手动控制机器鱼功能区（左手柄）

        /// <summary>
        /// 连接鱼
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectFishButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                spSend.SetInit(SendData2Fish.Function.SetMux, SendData2Fish.MuxType.Multi); //还没写Single的
                spSend.SetNetwork(0, SendData2Fish.NetType.TCP, data.LeftHandFishIP, data.LeftHandFishPort);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //更改文字，开启各种控件
            connectStateText.Text = "已连接";
            speedSlider.IsEnabled = true;
            switchColor1Button.IsEnabled = true;
            switchColor2Button.IsEnabled = true;
            turnForwardButton.IsEnabled = true;
            turnLeftButton.IsEnabled = true;
            turnRightButton.IsEnabled = true;
            //startStopVRControlButton.IsEnabled = startStopVRButton.Content as string == "停止监听VR"; //说明监听VR已经开始了
        }

        /// <summary>
        /// 逆时针方向（-）设置鱼颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchColor1Button_Click(object sender, RoutedEventArgs e)
        {
            spSend.SetColorCycle(0, '-');
        }

        /// <summary>
        /// 顺时针方向（+）设置鱼颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchColor2Button_Click(object sender, RoutedEventArgs e)
        {
            spSend.SetColorCycle(0, '+');
        }

        /// <summary>
        /// 左转按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnLeftButton_Click(object sender, RoutedEventArgs e)
        {
            spSend.SetMove(0, SendData2Fish.Direction.Left, (SendData2Fish.Speed)speedSlider.Value);
        }

        /// <summary>
        /// 前进按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnForwardButton_Click(object sender, RoutedEventArgs e)
        {
            spSend.SetMove(0, SendData2Fish.Direction.Forward, (SendData2Fish.Speed)speedSlider.Value);
        }

        /// <summary>
        /// 右转按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnRightButton_Click(object sender, RoutedEventArgs e)
        {
            spSend.SetMove(0, SendData2Fish.Direction.Right, (SendData2Fish.Speed)speedSlider.Value);
        }
        #endregion

        #region 手动控制机器鱼功能区（右手柄）

        /// <summary>
        /// 连接鱼
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectFishButton2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                spSend.SetInit(SendData2Fish.Function.SetMux, SendData2Fish.MuxType.Multi); //还没写Single的
                spSend.SetNetwork(1, SendData2Fish.NetType.TCP, data.RightHandFishIP, data.RightHandFishPort);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            //更改文字，开启各种控件
            connectStateText.Text = "已连接";
            speedSlider.IsEnabled = true;
            switchColor1Button2.IsEnabled = true;
            switchColor2Button2.IsEnabled = true;
            turnForwardButton2.IsEnabled = true;
            turnLeftButton2.IsEnabled = true;
            turnRightButton2.IsEnabled = true;
            //startStopVRControlButton.IsEnabled = startStopVRButton2.Content as string == "停止监听VR"; //说明监听VR已经开始了
        }

        /// <summary>
        /// 逆时针方向（-）设置鱼颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchColor1Button2_Click(object sender, RoutedEventArgs e) 
            => spSend.SetColorCycle(1, '-');

        /// <summary>
        /// 顺时针方向（+）设置鱼颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchColor2Button2_Click(object sender, RoutedEventArgs e) 
            => spSend.SetColorCycle(1, '+');

        /// <summary>
        /// 左转按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnLeftButton2_Click(object sender, RoutedEventArgs e) 
            => spSend.SetMove(1, SendData2Fish.Direction.Left, (SendData2Fish.Speed)speedSlider.Value);

        /// <summary>
        /// 前进按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnForwardButton2_Click(object sender, RoutedEventArgs e) 
            => spSend.SetMove(1, SendData2Fish.Direction.Forward, (SendData2Fish.Speed)speedSlider.Value);

        /// <summary>
        /// 右转按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TurnRightButton2_Click(object sender, RoutedEventArgs e) 
            => spSend.SetMove(1, SendData2Fish.Direction.Right, (SendData2Fish.Speed)speedSlider.Value);






        #endregion

        
    }

}
