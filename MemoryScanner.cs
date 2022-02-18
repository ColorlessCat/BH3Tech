using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace 对崩坏科研3
{


    public class MemoryScanner
    {
        //武器地址只可能是0x2或者0x4开头 游戏更新可能会变但应该不会在别的范围
        const int START_ADD = 0x20000000;
        const int END_ADD = 0x50000000;
        [DllImport("user32.dll")]
        extern static IntPtr FindWindow(string? lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern int GetWindowThreadProcessId(IntPtr hWnd, out int dwProcessId);
        [DllImport("PHMemReader.dll")]
        static extern int ReadProcessMemoryByDriverInt(int address, out Int64 readBytes);
        [DllImport("PHMemReader.dll")]
        static extern float ReadProcessMemoryByDriverFloat(int address, out Int64 readBytes);
        [DllImport("PHMemReader.dll")]
        static extern byte ReadProcessMemoryByDriverByte(int address, out Int64 readBytes);
        [DllImport("PHMemReader.dll")]
        static extern int WriteProcessMemoryByDriverInt(int address, out int value, out Int64 writeBytes);
        [DllImport("PHMemReader.dll")]
        static extern int WriteProcessMemoryByDriverFloat(int address, out float value, out Int64 writeBytes);
        [DllImport("PHMemReader.dll")]
        static extern int WriteProcessMemoryByDriverByte(int address, out byte value, out Int64 writeBytes);
        [DllImport("PHMemReader.dll", EntryPoint = "Init")]
        static extern void Init(int pid);

        public MemoryScanner()
        {
            int pid = 0;
            IntPtr pw = FindWindow(null, "崩坏3");
            GetWindowThreadProcessId(pw, out pid);
            if (pid == 0) throw new Exception();
            Init(pid);
        }

        public bool WriteMemory(int add, dynamic value)
        {
            Int64 writeBytes = 0;
            if (value is int)
            {
                int realValue;
                realValue = (int)value;
                WriteProcessMemoryByDriverInt(add, out realValue, out writeBytes);
            }
            else if (value is float)
            {
                float realValue;
                realValue = (float)value;
                WriteProcessMemoryByDriverFloat(add, out realValue, out writeBytes);
            }
            else if (value is byte)
            {
                byte realValue;
                realValue = (byte)value;
                WriteProcessMemoryByDriverByte(add, out realValue, out writeBytes);
            }
            return writeBytes != 0;
        }
        public T ReadMemory<T>(int add)
        {
            
            Type type = typeof(T);
            Int64 readBytes;
            dynamic result;
            if (type.Name.Equals("Int32"))
                result = ReadProcessMemoryByDriverInt(add, out readBytes);
            //TODO 这里读取float会损失精度(小数部分) 但是原生C++就正常 后面记得找原因
            else if (type.Name.Equals("Single"))
                result = ReadProcessMemoryByDriverFloat(add, out readBytes);
            else if (type.Name.Equals("Byte"))
                result = ReadProcessMemoryByDriverByte(add, out readBytes);
            else result = -1;
            return (T)result;
        }
        public delegate void AOBScanCallback(float progress, int result);
        //崩崩崩物品特征码专用
        public void AOBScan4BH3MultiThread(byte[] target, int addressLow3, AOBScanCallback callback)
        {
            int threadQuanlity = 4;
            int segementSize = (END_ADD - START_ADD) / threadQuanlity;
            int finishedCount = 0;
            for (int i = 0; i < threadQuanlity; i++)
            {
                int start = START_ADD + segementSize * i;
                int end=start + segementSize;
                new Thread(() =>
                {
                    int result;
                    if (addressLow3 == 0x00000fff)
                       result= AOBScan(start, end, target);
                    else
                       result= AOBScanWithLow3(start, end, target, addressLow3);
                    Interlocked.Increment(ref finishedCount);
                    callback(finishedCount*1f/threadQuanlity, result);  
                }).Start();  
            }

        }
        //有些物品的地址的低三位是可以确定的 因此加快搜索速度 
        public int AOBScanWithLow3(int start,int end,byte[] target, int addressLow3)
        {
            
            //低位变0 从第四个16进制位开始加
          
            uint realStart=uint.Parse(start.ToString());
            realStart = realStart & 0x7ffff000;
            start = (int)realStart;
            start += addressLow3;
            int result=-1;
            for (int i = start; i < end; i+=16*16*16)
            {
               
                byte b = ReadMemory<byte>(i);
                if (b != target[0]) continue;
                int k = 1;
                for(; k < target.Length; k++)
                {
                    if (ReadMemory<byte>(i + k) != target[k]) break;
                }
                if (k != target.Length) continue;
                result = i;
            }
            return result;
        }
        public int AOBScan(int start,int end,byte[] target)
        {
            int result = -1;
            for (int i = start; i < end; i+=4)
            {
                byte b = ReadMemory<byte>(i);
                if (b != target[0]) continue;
                int k = 1;
                for (; k < target.Length; k++)
                {
                    if (ReadMemory<byte>(i + k) != target[k]) break;
                }
                //认为上一块内存找到了前边部分匹配的字节 这块内存的后半部分就不可能和目标的前半部分匹配
                //极少数情况可能会因此漏掉 但是基本不可能
                i += target.Length - target.Length % 4;
                if (k != target.Length) continue;
                result = i;
                break;
            }
            return result;
        }


    }
}
