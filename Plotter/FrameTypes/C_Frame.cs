using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTG_Comms
{
    public class C_Frame : CTGframe
    {
        public const int SamplesPerFrame = 4;

        public byte       C        { get; private set; } = 0;
        public CTGStatus? Status   { get; private set; } 
        public double[]   HR1      { get; } = [ 0,0,0,0 ];
        public double[]   HR2      { get; } = [ 0,0,0,0 ];
        public double[]   MHR      { get; } = [ 0,0,0,0 ];
        public double[]   TOCO     { get; } = [ 0,0,0,0 ];
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
                var status = ReadWord();
                ReadArray(HR1, ReadHR);
                ReadArray(HR2, ReadHR);
                ReadArray(MHR, ReadHR);
                ReadArray(TOCO, ReadTOCO);
                HRmode = ReadWord();
                TOCOmode = ReadByte();
                SpO2 = ReadByte();

                Status = new(this);
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
