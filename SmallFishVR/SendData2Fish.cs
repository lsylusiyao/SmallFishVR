﻿using System;
using System.IO.Ports;
using System.Threading;

namespace SmallFishVR
{
    //VR控制的时候不能太快，不然串口线程的sleep时间挺长的

    /// <summary>
    /// 将发送给鱼的数据再次封装，成为简单易懂的数据
    /// TODO：没有写单连接的，因此一定要用多连接……
    /// </summary>
    public class SendData2Fish : BluetoothLE
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
            //Right = 0x02,
            //Left = 0x03 //感觉这里是反了的，因此换过来
            Left = 0x02,
            Right = 0x03
        }

        public SendData2Fish(string serviceGuid, string writeGuid, string readGuid) : base(serviceGuid, writeGuid, readGuid) { }

        /// <summary>
        /// 设置颜色（发送数据）
        /// </summary>
        /// <param name="color">颜色</param>
        public void SetColor(Color color)
        {
            byte[] byteBuffer = new byte[7];
            baseCmd.CopyTo(byteBuffer, 0);
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
        /// <param name="dir">只能写'+'或者'-'，否则会报错，'+'为颜色序号增加，'-'为减少，列表见枚举</param>
        /// <param name="isChangedColor">限制是否执行这个函数，true表示不再执行</param>
        public void SetColorCycle(char dir, bool isChangedColor = false)
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
                SetColor(tempColor);
            }

        }

        /// <summary>
        /// 设置运动方向和速度（包括停止）
        /// </summary>
        /// <param name="direction">方向</param>
        /// <param name="speed">速度</param>
        /// <param name="isSentFlag">限制是否执行这个函数，true表示不再执行</param>
        public void SetMove(Direction direction, Speed speed = Speed.VeryLow, bool isSentFlag = false)
        {
            if(!isSentFlag)
            {
                if (direction == Direction.Stop) return; //可能这个鱼发送stop不好用，只能停止发送了
                byte[] byteBuffer = new byte[8];
                baseCmd.CopyTo(byteBuffer, 0);
                byteBuffer[4] = 0x00;
                byteBuffer[5] = (byte)direction;
                byteBuffer[6] = (byte)speed;
                byteBuffer[7] = 0xFF;
                Write(byteBuffer, 0, 8);
            }
            
        }

        /// <summary>
        /// 重启鱼
        /// </summary>
        public void Reset()
        {
            byte[] byteBuffer = new byte[6];
            baseCmd.CopyTo(byteBuffer, 0);
            byteBuffer[4] = 0x04;
            byteBuffer[5] = 0xFF;
            Write(byteBuffer, 0, 6);
            Thread.Sleep(10);
        }
    }
}
