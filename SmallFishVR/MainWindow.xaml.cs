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
    enum WhichOne //解析数据数组的时候用
    {
        POSX, POSY, POSZ, EULAX, EULAY, EULAZ, STAX, STAY
    };
    
    public class DataStore //专门存数据的
    {
        
        public List<string> SpList { get; set; }
        public List<int> BaudRate { get; set; }
        public List<int> DataBits { get; set; }
        public List<double> StopBits { get; set; }
        public List<string> Parity { get; set; }
        public RealPortInfo Real;

        public struct RealPortInfo
        {
            public string portName;
            public int baudRate;
            public int dataBits;
            public StopBits stopBits;
            public Parity parity;
        };

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
        private BridgeClass bridge;
        DataStore data = new DataStore();
        SerialPort sp = new SerialPort();
        Thread runVRThread;
        Thread listenVRThread;
        Thread listenSPThread;
        public MainWindow()
        {
            InitializeComponent();
            data.Init();
            Bindings();
            DefaultSettings();
            
        }
        
        void Bindings()
        {
            portBox.DataContext = data;
            baudRateBox.DataContext = data;
            parityBox.DataContext = data;
            dataBitsBox.DataContext = data;
            stopBitsBox.DataContext = data;
        }

        void DefaultSettings()
        {
            //portBox.SelectedIndex = 0 ;
            baudRateBox.SelectedIndex = 3;
            parityBox.SelectedIndex = 0;
            dataBitsBox.SelectedIndex = 3;
            stopBitsBox.SelectedIndex = 0;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (sp.IsOpen) sp.Close();
        }

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

        private void InitVRButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("即将初始化VR，请确认VR硬件已经连接，Steam已经打开。否则程序会自动退出" +
                "\n选择\"是\"来继续，\"否\"来返回", "VR连接提示",MessageBoxButton.YesNo,MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                bridge = new BridgeClass();
                startStopVRButton.IsEnabled = true;
            }
            else if (result == MessageBoxResult.No) return;
        }


        private void ExitButton_Click(object sender, RoutedEventArgs e) => Close();

        private void ReCountCOMButton_Click(object sender, RoutedEventArgs e)
        {
            data.SpList = new List<string>(SerialPort.GetPortNames());
            MessageBox.Show("重新检查完毕", "端口列表");
        }

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
                    sp.Open();
                    openClosePortButton.Content = "关闭端口";
                    portStateText.Text = "已打开";
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

        private void StartStopVRButton_Click(object sender, RoutedEventArgs e)
        {
            bridge.Run();
        }
    }
}
