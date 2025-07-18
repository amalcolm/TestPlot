using System;
using System.Collections.Generic;

namespace Plotter
{
    public class BaseFrame
    {
        protected BaseFrame() 
        {
            IsComplete  = false;
            IsMalformed = false;
        }
       
        public virtual bool ProcessFrame() => IsComplete;

        public bool      IsComplete    { get; set; }
        public bool      IsMalformed   { get; set; }

        protected readonly List<byte> bytes = [];
        protected ushort crc = 0;

        protected int dateIndex = 0;
        protected int dataLen = 0;
        protected byte[] data = [];

        protected byte ReadByte()
        {
            if (dateIndex >= dataLen) { IsMalformed = true; return 0; }
            return data[dateIndex++];
        }
        protected ushort ReadWord()
        {
            if (dateIndex + 1 >= dataLen) { IsMalformed = true; return 0; }
            return (ushort)((data[dateIndex++] << 8) | data[dateIndex++]);
        }



        protected void ReadArray(double[] arr, Func<double> reader)
        {
            int N = arr.Length;  if (N == 0) return;

            for (int i = 0; i < N && !IsMalformed; i++)
                arr[i] = reader();
        }




        protected void UpdateCRC(byte b)
        {
            crc = (ushort)((crc << 8) ^ CRCTable[(crc >> 8) ^ b]);
        }

        protected byte[] ToArrayCRC()
        {
            byte[] frame = new byte[bytes.Count + 2];
            Array.Copy(bytes.ToArray(), frame, bytes.Count);

            frame[bytes.Count] = (byte)(crc >> 8);
            frame[bytes.Count + 1] = (byte)(crc & 0xFF);

            return frame;
        }

        protected static readonly ushort[] CRCTable;

        public const byte DLE = 0x10;
        public const byte STX = 0x02;
        public const byte ETX = 0x03;


        static BaseFrame()
        {
            const ushort GENERATE_POLYNOMIAL = 0x1021;

            CRCTable = new ushort[256];
            for (int i = 0; i < 256; i++)
            {
                ushort value = 0;
                ushort temp = (ushort)(i << 8);
                for (int j = 0; j < 8; j++)
                {
                    if (((value ^ temp) & 0x8000) != 0)
                        value = (ushort)((value << 1) ^ GENERATE_POLYNOMIAL);
                    else
                        value <<= 1;
                    temp <<= 1;
                }
                CRCTable[i] = value;
            }
        }

    }
}
