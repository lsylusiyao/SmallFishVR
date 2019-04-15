using System;
using System.IO.Ports;
using System.Collections.Generic;
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
        private BridgeClass bridge;
        SerialPort sp = new SerialPort();
        string[] spList;
        public MainWindow()
        {
            InitializeComponent();
            spList = SerialPort.GetPortNames();
        }

        private void InitVRButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("即将初始化VR，请确认VR硬件已经连接，Steam已经打开。否则程序会自动退出" +
                "\n选择\"是\"来继续，\"否\"来返回", "VR连接提示",MessageBoxButton.YesNo,MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes) bridge = new BridgeClass();
            else if (result == MessageBoxResult.No) return;
        }

        private void ReCountCOMButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
