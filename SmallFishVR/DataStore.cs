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
        /// 定义是哪个手柄控制单个鱼（如果控制两个鱼会删除这个）
        /// </summary>
        private readonly List<string> whichHand = new List<string> { "leftHand", "rightHand" };

        /// <summary>
        /// 获得的本机所有SerialPort名称
        /// </summary>
        public List<string> SpList { get; set; }

        /// <summary>
        /// 波特率
        /// </summary>
        public List<int> BaudRate { get; set; }

        /// <summary>
        /// 数据位数
        /// </summary>
        public List<int> DataBits { get; set; }

        /// <summary>
        /// 停止位
        /// </summary>
        public List<double> StopBits { get; set; }

        /// <summary>
        /// 校验位
        /// </summary>
        public List<string> Parity { get; set; }

        /// <summary>
        /// HMD的所有数据
        /// </summary>
        public List<double> HMDData { get; set; }

        /// <summary>
        /// 左手柄的所有数据
        /// </summary>
        public List<double> LeftHandData { get; set; }

        /// <summary>
        /// 右手柄的所有数据
        /// </summary>
        public List<double> RightHandData { get; set; }

        /// <summary>
        /// 选择是哪个手柄
        /// </summary>
        public List<string> WhichHand => whichHand;
        
        /// <summary>
        /// 选择的手柄的数据（指针）
        /// </summary>
        public List<double> HandData { get; set; }
        

        /// <summary>
        /// 定义结构的对象
        /// </summary>
        public RealPortInfo Real;

        private List<double> a = new List<double> { 1, 2, 3, 4, 5 };
        public List<double> A { get => a; set => a = value; }

        //控制的时候，方向数据实时解析发送就行（单独写一个运动类），颜色使用顺时针逆时针控制，一次是切换一种颜色，用一个flag，
        //切换完了就不再使用，直到复原之后
        //写一个发送数据封装类（enum或者字符串解析为命令），一个解析VR运动类（尺度，方向，应该返回哪种东西），

        /// <summary>
        /// 初始化数据列表
        /// </summary>
        public void Init()
        {
            SpList = new List<string>(SerialPort.GetPortNames());
            BaudRate = new List<int> { 9600, 19200, 38400, 115200 };
            DataBits = new List<int> { 5, 6, 7, 8 };
            StopBits = new List<double> { 1, 1.5, 2 };
            Parity = new List<string> { "NONE", "ODD", "EVEN" };
        }
    }
}
