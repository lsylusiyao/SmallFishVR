using System;
using System.IO.Ports;
using System.Threading;

namespace SmallFishVR
{
    //VR控制的时候不能太快，不然串口线程的sleep时间挺长的

    /// <summary>
    /// 将发送给鱼的数据再次封装，成为简单易懂的数据
    /// TODO：没有写单连接的，因此一定要用多连接……
    /// </summary>
    public class SendData2Fish : SerialPort
    {
        /// <summary>
        /// 发送的数据的头部，是固定内容
        /// </summary>
        private readonly byte[] baseCmd = new byte[4] { 0x55, 0xAA, 0x99, 0x11 };

        /// <summary>
        /// 当前颜色
        /// </summary>
        private Color colorNow = Color.Blue;

        /// <summary>
        /// 功能的枚举
        /// </summary>
        public enum Function
        {
            GetIPs, CheckStatus, SetMux
        }

        /// <summary>
        /// 单通信还是多通信，None是占位置用的
        /// </summary>
        public enum MuxType
        {
            Single, Multi, None
        }

        /// <summary>
        /// 网络类型枚举
        /// </summary>
        public enum NetType
        {
            TCP, UDP
        }

        /// <summary>
        /// 颜色枚举
        /// </summary>
        public enum Color
        {
            Black = 0x00, Blue, Green, Blue_Green, Red, Pink, Yellow, White = 0x07
        }

        /// <summary>
        /// 速度枚举
        /// </summary>
        public enum Speed
        {
            VeryLow = 0x00, Low, Medium, High = 0x03
        }

        /// <summary>
        /// 方向枚举（包括停止）
        /// </summary>
        public enum Direction
        {
            Forward = 0x00,
            Stop = 0x01,
            Right = 0x02,
            Left = 0x03
        }

        /// <summary>
        /// 初始化设置，可以查看IP，并且设置连接类型
        /// </summary>
        /// <param name="function">功能</param>
        /// <param name="muxType">单/多连接</param>
        public void SetInit(Function function, MuxType muxType = MuxType.None)
        {
           switch(function)
            {
                case Function.GetIPs: WriteLine("AT+CWLIF\r\n"); break; //查看接入设备的IP
                case Function.CheckStatus: WriteLine("AT+CIPSTATUS\r\n"); break;
                case Function.SetMux:
                    if (muxType == MuxType.None) throw new Exception("程序编写错误");
                    else WriteLine("AT+CIPMUX=" + Convert.ToString((int)muxType) + "\r\n"); //设置单连接还是多连接
                    break;
                default: throw new Exception("程序编写错误");
            }
            Thread.Sleep(10);
        }

        /// <summary>
        /// 设置网络信息，TCP or UDP，IP，端口等
        /// </summary>
        /// <param name="which">选择哪一个鱼</param>
        /// <param name="netType">网络连接类型</param>
        /// <param name="IP">IP地址</param>
        /// <param name="port">端口</param>
        public void SetNetwork(int which, NetType netType, string IP, int port)
        {
            string cmd = "AT+CIPSTART=" + which.ToString() + ",\"";
            cmd += netType.ToString();
            cmd += "\",\"" + IP + "\"," + port.ToString() + "\r\n";
            WriteLine(cmd);
            Thread.Sleep(10);
        }

        /// <summary>
        /// 设置颜色（发送数据）
        /// </summary>
        /// <param name="which">选择哪一个鱼</param>
        /// <param name="color">颜色</param>
        public void SetColor(int which, Color color)
        {
            byte[] byteBuffer = new byte[7];
            baseCmd.CopyTo(byteBuffer, 0);
            string cmd = "AT+CIPSEND=" + which.ToString() + ",7\r\n";
            WriteLine(cmd);
            Thread.Sleep(40);
            byteBuffer[4] = 0x05;
            byteBuffer[5] = Convert.ToByte(color);
            byteBuffer[6] = 0xFF;
            colorNow = color;
            Write(byteBuffer, 0, 7);
            Thread.Sleep(10);
        }

        /// <summary>
        /// 设置颜色的循环轮回（调用SetColor）
        /// </summary>
        /// <param name="which">选择哪一个鱼</param>
        /// <param name="dir">只能写'+'或者'-'，否则会报错，'+'为颜色序号增加，'-'为减少，列表见枚举</param>
        /// <param name="isChangedColor">限制是否执行这个函数，true表示不再执行</param>
        public void SetColorCycle(int which, char dir, bool isChangedColor = false)
        {            
            if(!isChangedColor)
            {
                int temp = (int)colorNow;
                Color tempColor;
                switch (dir)
                {
                    case '+':
                        temp++;
                        tempColor = temp >= 8 ? (Color)temp - 8 : (Color)temp;
                        break;
                    case '-':
                        temp--;
                        tempColor = temp < 0 ? (Color)temp + 8 : (Color)temp;
                        break;
                    default:
                        throw new Exception("程序编写错误");
                }
                SetColor(which, tempColor);
            }

        }

        /// <summary>
        /// 设置运动方向和速度（包括停止）
        /// </summary>
        /// <param name="which">选择哪一个鱼</param>
        /// <param name="direction">方向</param>
        /// <param name="speed">速度</param>
        /// <param name="isSentFlag">限制是否执行这个函数，true表示不再执行</param>
        public void SetMove(int which, Direction direction, Speed speed, bool isSentFlag = false)
        {
            if(!isSentFlag)
            {
                WriteLine("AT+CIPSEND=" + which.ToString() + ",8\r\n");
                Thread.Sleep(40);
                byte[] byteBuffer = new byte[8];
                baseCmd.CopyTo(byteBuffer, 0);
                byteBuffer[4] = 0x00;
                byteBuffer[5] = (byte)direction;
                byteBuffer[6] = (byte)speed;
                byteBuffer[7] = 0xFF;
                Write(byteBuffer, 0, 8);
                Thread.Sleep(10);
            }
            
        }

        /// <summary>
        /// 重启鱼
        /// </summary>
        /// <param name="which">选择哪一个鱼</param>
        public void Reset(int which)
        {
            WriteLine("AT+CIPSEND=" + which.ToString() + ",6\r\n");
            Thread.Sleep(40);
            byte[] byteBuffer = new byte[6];
            baseCmd.CopyTo(byteBuffer, 0);
            byteBuffer[4] = 0x04;
            byteBuffer[5] = 0xFF;
            Write(byteBuffer, 0, 6);
            Thread.Sleep(10);
        }
    }
}
