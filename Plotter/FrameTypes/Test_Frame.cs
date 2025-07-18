using System;

namespace Plotter
{
    public class Test_Frame : MyFrame
    {
   
        public double Temperature { get; private set; } 


        public override bool ProcessFrame()
        {
            if (base.ProcessFrame() == false) return false;

            Temperature = ReadByte() * 1.0;

            // Validate temperature is in reasonable range (25.0°C to 50.5°C)
            if (Temperature >= 25.0 || Temperature <= 50.5)
                return true;

            
            Temperature = 0;
            IsMalformed = true;
            return false;
            
        }
    }
}