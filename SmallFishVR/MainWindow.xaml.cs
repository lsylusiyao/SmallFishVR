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
    /// 存放数据的类，MainWindows中新建一个对象以使用其中的数据
    /// </summary>
    public class DataStore
    {
        public struct RealPortInfo
        {
            public string portName;
            public int baudRate;
            public int dataBits;
            public StopBits stopBits;
            public Parity parity;
        };
        public List<string> SpList { get; set; }
        public List<int> BaudRate { get; set; }
        public List<int> DataBits { get; set; }
        public List<double> StopBits { get; set; }
        public List<string> Parity { get; set; }
        
        
        public RealPortInfo Real;
        
        public void Init()
        {
            SpList = new List<string>(SerialPort.GetPortNames());
            BaudRate = new List<int> { 9600, 19200, 38400, 115200 };
            DataBits = new List<int> { 5, 6, 7, 8 };
            StopBits = new List<double> { 1, 1.5, 2 };
            Parity = new List<string> { "NONE", "ODD", "EVEN" };
        }
    }
    
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private BridgeClass bridge; //新建一个VR类（从CLR）
        DataStore data = new DataStore(); //新建数据存储对象
        SerialPort sp = new SerialPort(); //新建串口对象
        public delegate void UpdateDataInvoke(string s); //更新数据的委托
        Thread VRthread; //VR线程
        Thread listenVRThread; //监听VR数据线程，在有错的时候也会报错
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
            portBox.DataContext = data;
            baudRateBox.DataContext = data;
            parityBox.DataContext = data;
            dataBitsBox.DataContext = data;
            stopBitsBox.DataContext = data;
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
        }
        /// <summary>
        /// 重写关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            if (sp.IsOpen) sp.Close();
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
            switch(parityBox.SelectedIndex)
            {
                case 0:
                    data.Real.parity = Parity.None;
                    break;
                case 1:
                    data.Real.parity = Parity.Odd;
                    break;
                case 2:
                    data.Real.parity = Parity.Even;
                    break;
                default:
                    MessageBox.Show("Error：参数不正确!", "Error");
                    break;
            }
        }
        /// <summary>
        /// 手动创建一个关闭按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitButton_Click(object sender, RoutedEventArgs e) => Close();
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
                if (!sp.IsOpen)
                {
                    Switch2RealInfo();
                    sp.PortName = data.Real.portName;
                    sp.BaudRate = data.Real.baudRate;
                    sp.StopBits = data.Real.stopBits;
                    sp.DataBits = data.Real.dataBits;
                    sp.Parity = data.Real.parity;
                    sp.DtrEnable = true;
                    sp.RtsEnable = true;
                    sp.ReadTimeout = 1000; //miliseconds

                    sp.Close(); //为了防止之前端口打开，因此先关闭

                    sp.DataReceived += new SerialDataReceivedEventHandler(Sp_DataReceived); //收到数据的时候激活这个事件

                    sp.Open();
                    openClosePortButton.Content = "关闭端口"; //更改显示文字
                    portStateText.Text = "已打开"; //更改下方文字
                    connectFishButton.IsEnabled = true;
                }
                else
                {
                    sp.Close();
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
        /// 接收到数据时的事件，主要就是在SPDataBox中添加文本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Sp_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var boxSet = new UpdateDataInvoke(
                delegate (string r) { SPDataBox.AppendText(r + "\r\n"); });
            try
            {
                boxSet.Invoke(sp.ReadLine());
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message, "出错提示");
            }

        }

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
                bridge = new BridgeClass();
                startStopVRButton.IsEnabled = true;
                VRthread = new Thread(bridge.Run);
                listenVRThread = new Thread(listenVR);
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
                VRthread.Start();
                startStopVRButton.Content = "停止监听VR";
            }
            else
            {
                bridge.SetKeepVRWorking(false);
                bridge.Dispose();
                startStopVRButton.IsEnabled = false;
                startStopVRButton.Content = "开始监听VR";
            }
        }

        private void listenVR()
        {
            while(VRthread.IsAlive)
            {
                // HMD数据解析


                //leftHand数据解析
            }
        }
    }
}
