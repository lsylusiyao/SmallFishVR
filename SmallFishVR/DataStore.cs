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

        public double[] HMDZero { set; get; } = new double[6];
        public double[] LeftHandZero { set; get; } = new double[8];
        public double[] RightHandZero { set; get; } = new double[8];

        public double[] HMDDataOrigin { set; get; }
        public double[] LeftHandDataOrigin { set; get; }
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

        private List<double> a = new List<double> { 1, 2, 3, 4, 5 };
        public List<double> A { get => a; set => a = value; }

        //控制的时候，方向数据实时解析发送就行（单独写一个运动类），颜色使用顺时针逆时针控制，一次是切换一种颜色，用一个flag，
        //切换完了就不再使用，直到复原之后
        //写一个发送数据封装类（enum或者字符串解析为命令），一个解析VR运动类（尺度，方向，应该返回哪种东西），

    }
}
