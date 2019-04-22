using System.IO.Ports;
using System.Collections.Generic;

namespace SmallFishVR
{
    /// <summary>
    /// 存放数据的类，MainWindows中新建一个对象以使用其中的数据
    /// </summary>
    public class DataStore
    {
        /// <summary>
        /// 用来存储真正的串口需要的数据，本质是数据转换
        /// </summary>
        public struct RealPortInfo
        {
            public string portName;
            public int baudRate;
            public int dataBits;
            public StopBits stopBits;
            public Parity parity;
        };

        /// <summary>
        /// 获得的本机所有SerialPort名称
        /// </summary>
        public List<string> SpList { get; set; } = new List<string>(SerialPort.GetPortNames());

        /// <summary>
        /// 波特率
        /// </summary>
        public List<int> BaudRate { get; } = new List<int> { 9600, 19200, 38400, 115200 };

        /// <summary>
        /// 数据位数
        /// </summary>
        public List<int> DataBits { get; } = new List<int> { 5, 6, 7, 8 };

        /// <summary>
        /// 停止位
        /// </summary>
        public List<double> StopBits { get; } = new List<double> { 1, 1.5, 2 };

        /// <summary>
        /// 校验位
        /// </summary>
        public List<string> Parity { get; } = new List<string> { "NONE", "ODD", "EVEN" };

        /// <summary>
        /// 头盔数据零点记录
        /// </summary>
        public double[] HMDZero { set; get; } = new double[6];

        /// <summary>
        /// 左手柄数据零点记录
        /// </summary>
        public double[] LeftHandZero { set; get; } = new double[8];

        /// <summary>
        /// 右手柄数据零点记录
        /// </summary>
        public double[] RightHandZero { set; get; } = new double[8];

        /// <summary>
        /// 头盔原始数据
        /// </summary>
        public double[] HMDDataOrigin { set; get; }

        /// <summary>
        /// 左手柄原始数据
        /// </summary>
        public double[] LeftHandDataOrigin { set; get; }

        /// <summary>
        /// 右手柄原始数据
        /// </summary>
        public double[] RightHandDataOrigin { set; get; }

        /// <summary>
        /// 定义结构的对象
        /// </summary>
        public RealPortInfo Real;

        /// <summary>
        /// 左手柄右手柄鱼的IP和端口
        /// </summary>
        public string LeftHandFishIP { get; set; }
        public string RightHandFishIP { get; set; }
        public int LeftHandFishPort { get; set; }
        public int RightHandFishPort { get; set; }

    }
}
