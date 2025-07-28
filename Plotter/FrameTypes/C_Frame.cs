using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plotter
{
    public class C_Frame : MyFrame
    {
        public const int SamplesPerFrame = 4;

        public byte       C        { get; private set; } = 0;
        public ushort     HRmode   { get; private set; } = 0;
        public byte       TOCOmode { get; private set; } = 0;
        public ushort     SpO2     { get; private set; } = 0;
                        
        public ushort  _statusWord { get; private set; }

        public override bool ProcessFrame()
        {
            if (base.ProcessFrame() == false) return false;

            C = dateIndex < dataLen ? data[dateIndex++] : (byte)0;

            if (C == 'C')  // 0x43 for CTG data
            {
                var _ = ReadWord();
                HRmode = ReadWord();
                TOCOmode = ReadByte();
                SpO2 = ReadByte();
            }
            else
            {
                IsMalformed = true;
                return false;
            }


            return true;
            
        }
    
    }
}
