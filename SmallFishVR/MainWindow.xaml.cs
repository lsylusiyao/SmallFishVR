using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BridgeDll;

namespace SmallFishVR
{
    
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 定义区
        private BridgeClass bridge; //新建一个VR类（从CLR）
        DataStore data = new DataStore(); //新建数据存储对象
        SendData2Fish spSend = new SendData2Fish(); //新建继承的带处理数据的串口对象
        public delegate void UpdateDataInvoke(string s); //更新数据的委托
        public string TempStr { get; set; }
        private bool isVRControlStart = false;
        
        Thread listenSPDataThread; //监听串口数据的线程
        Thread VRThread; //VR线程
        Thread listenVRThread; //监听VR数据线程，在有信息传来的时候也会报错
        Thread VRControlFishThread; //VR控制鱼的运动方向的控制
        #endregion

        #region 窗口主要功能区

        public MainWindow()
        {
            InitializeComponent();
            data.Init();
            Bindings();
            DefaultSettings();
            
        }
        /// <summary>
        /// 数据绑定类，所有的数据绑定类都写在这里
        /// </summary>
        void Bindings()
        {
            setPortGrid.DataContext = data;
            VRGrid.DataContext = data;
        }
        /// <summary>
        /// 缺省设置，默认选择的Index，暂时写在这里
        /// </summary>
        void DefaultSettings()
        {
            //portBox.SelectedIndex = 0 ;
            baudRateBox.SelectedIndex = 3;
            parityBox.SelectedIndex = 0;
            dataBitsBox.SelectedIndex = 3;
            stopBitsBox.SelectedIndex = 0;
            TempStr = string.Empty;
        }
        /// <summary>
        /// 重写关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (spSend.IsOpen) spSend.Close();
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

                    var updateData = new UpdateDataInvoke( //一个委托，用来向图形界面添加数据的
                        delegate (string s) { SPDataBox.AppendText(s + "\r\n"); });
                    
                    listenSPDataThread = new Thread(()=> //一个线程，用来将数据实时发送给委托的
                    {
                        while(true)
                        {
                            if (TempStr.Length > 0)
                            {
                                updateData(TempStr);
                                lock (this) {TempStr = string.Empty; }
                            }
                        }
                    });
                    spSend.Open();
                    listenSPDataThread.Start();
                    openClosePortButton.Content = "关闭端口"; //更改显示文字
                    portStateText.Text = "已打开"; //更改下方文字
                    connectFishButton.IsEnabled = true;
                }
                else
                {
                    spSend.Close();
                    listenSPDataThread.Abort();
                    openClosePortButton.Content = "打开端口";
                    portStateText.Text = "未打开";
                    connectFishButton.IsEnabled = false;
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
                    spSend.DiscardInBuffer();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "出错提示",MessageBoxButton.OK, MessageBoxImage.Error);
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
                    startStopVRButton.IsEnabled = true;
                    VRThread = new Thread(bridge.Run);
                    listenVRThread = new Thread(ListenVRThread);
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
        /// 开启关闭VR的按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartStopVRButton_Click(object sender, RoutedEventArgs e)
        {
            if (!bridge.GetKeepVRWorking())
            {
                VRThread.Start();
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
            while(VRThread.IsAlive)
            {
                // 绑定只能用List之类的类型（集成INotifyPropertyChanged的）
                // TODO：这里会不会因为更新了指针指向而不更新？
                data.HMDData = new List<double>(bridge.GetHMD());
                data.LeftHandData = new List<double>(bridge.GetLeftHand());
                data.RightHandData = new List<double>(bridge.GetRightHand());
                if(bridge.GetIsStrGiven())
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

        private void StartVRControlButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isVRControlStart)
            {
                VRControlFishThread = new Thread(VRControlFishThreadFunc);
                VRControlFishThread.Start();
                isVRControlStart = true;
                startVRControlButton.Content = "停止VR控制";
                MessageBox.Show("VR控制机器鱼已经开始，手动控制仍然生效", "提示");
            }
            else
            {
                VRControlFishThread.Abort();
                isVRControlStart = false;
                startVRControlButton.Content = "开始VR控制";
                MessageBox.Show("VR控制机器鱼已经停止", "提示");
            }

        }

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
            int[] divisionPoint = new int[] { 5, 25, 45, 65 }; //Stop-1-2-3-4的分界角度点
            bool isStop = false;
            //bool isSent = false;
            bool isChangedColor = false;
            while (true)
            {
                Thread.Sleep(100);
                #region 设置颜色部分
                if (data.HandData[4] < divisionPoint[0] && data.HandData[4] > -divisionPoint[0]) { isChangedColor = false; }
                else
                {
                    spSend.SetColorCycle(0, data.HandData[4] > 0 ? '-' : '+', isChangedColor);
                    isChangedColor = true;
                }
                #endregion

                if (data.HandData[3] < divisionPoint[0] && data.HandData[3] > -divisionPoint[0] &&
                    data.HandData[5] < divisionPoint[0] && data.HandData[5] > -divisionPoint[0])
                {
                    spSend.SetMove(0, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                    //isSent = false;
                    isStop = false;
                } //这里速度随便给
                else
                {
                    if (data.HandData[3] > divisionPoint[0]) //左右任意，只要前后足够靠后
                    {
                        spSend.SetMove(0, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                        isStop = true;
                        continue;
                    }
                    #region 左右转发送数据
                    //只有左右方向恢复，鱼才会前进运动
                    if (data.HandData[5] <= divisionPoint[1] || data.HandData[5] >= -divisionPoint[1])
                    {
                        spSend.SetMove(0,
                          data.HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                          SendData2Fish.Speed.VeryLow, isStop);
                        //isSent = true;
                    }
                    else if (data.HandData[5] <= divisionPoint[2] || data.HandData[5] >= -divisionPoint[2])
                    {
                        spSend.SetMove(0,
                          data.HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                          SendData2Fish.Speed.Low, isStop);
                        continue;
                        //isSent = true;
                    }
                    else if (data.HandData[5] <= divisionPoint[3] || data.HandData[5] >= -divisionPoint[3])
                    {
                        spSend.SetMove(0,
                          data.HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                          SendData2Fish.Speed.Medium, isStop);
                        continue;
                        //isSent = true;
                    }
                    else
                    {
                        spSend.SetMove(0,
                          data.HandData[5] > 0 ? SendData2Fish.Direction.Left : SendData2Fish.Direction.Right,
                          SendData2Fish.Speed.High, isStop);
                        continue;
                        //isSent = true;
                    }
                    #endregion

                    #region 前进发送数据

                    if (data.HandData[3] > -divisionPoint[0] && data.HandData[3] < 0)
                    {
                        //这条其实不写也行？
                        spSend.SetMove(0, SendData2Fish.Direction.Stop, SendData2Fish.Speed.VeryLow);
                    }
                    else if (data.HandData[3] > -divisionPoint[1])
                    { spSend.SetMove(0, SendData2Fish.Direction.Forward, SendData2Fish.Speed.Low); }
                    else if (data.HandData[3] > -divisionPoint[2])
                    { spSend.SetMove(0, SendData2Fish.Direction.Forward, SendData2Fish.Speed.Medium); }
                    else { spSend.SetMove(0, SendData2Fish.Direction.Forward, SendData2Fish.Speed.High); }
                    
                    #endregion
                }
            }
        }
        #endregion

        #region 手动控制机器鱼功能区
        private void CheeckStateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                spSend.SetInit(SendData2Fish.Function.GetIPs);
                spSend.SetInit(SendData2Fish.Function.CheckStatus);
                MessageBox.Show("检查完成，请查看返回信息状态和IP", "提示信息", MessageBoxButton.OKCancel);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void ConnectFishButton_Click(object sender, RoutedEventArgs e)
        {
            switch (switchHandBox.SelectedIndex)
            {
                case 0:
                    data.HandData = data.LeftHandData;
                    break;
                case 1:
                    data.HandData = data.RightHandData;
                    break;
                default:
                    MessageBox.Show("未选择哪一个手柄，请选择手柄", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
            }
            try
            {
                spSend.SetInit(SendData2Fish.Function.SetMux, SendData2Fish.MuxType.Multi); //还没写Single的
                spSend.SetNetwork(0, SendData2Fish.NetType.TCP, "192.168.4.2", 1001);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error:" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            connectStateText.Text = "已连接";
            speedSlider.IsEnabled = true;
            switchColor1Button.IsEnabled = true;
            switchColor2Button.IsEnabled = true;
            turnForwardButton.IsEnabled = true;
            turnLeftButton.IsEnabled = true;
            turnRightButton.IsEnabled = true;
            startVRControlButton.IsEnabled = true;
        }

        private void SwitchColor1Button_Click(object sender, RoutedEventArgs e)
        {
            spSend.SetColorCycle(0, '-');
        }

        private void SwitchColor2Button_Click(object sender, RoutedEventArgs e)
        {
            spSend.SetColorCycle(0, '+');
        }

        private void TurnLeftButton_Click(object sender, RoutedEventArgs e)
        {
            spSend.SetMove(0, SendData2Fish.Direction.Left, (SendData2Fish.Speed)speedSlider.Value);
        }

        private void TurnForwardButton_Click(object sender, RoutedEventArgs e)
        {
            spSend.SetMove(0, SendData2Fish.Direction.Forward, (SendData2Fish.Speed)speedSlider.Value);
        }

        private void TurnRightButton_Click(object sender, RoutedEventArgs e)
        {
            spSend.SetMove(0, SendData2Fish.Direction.Right, (SendData2Fish.Speed)speedSlider.Value);
        }
        #endregion

    }
}
