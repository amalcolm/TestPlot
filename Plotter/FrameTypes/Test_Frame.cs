using System;

namespace CTG_Comms
{
    public class Test_Frame : CTGframe
    {
        private const double TEMPERATURE_OFFSET     = 25.0;  // Base temperature offset in Celsius
        private const double TEMPERATURE_RESOLUTION =  0.1;  // Each unit represents 0.1°C

        public double Temperature { get; private set; } 


        public override bool ProcessFrame()
        {
            if (base.ProcessFrame() == false) return false;
            
            Temperature = ReadByte() * TEMPERATURE_RESOLUTION +  TEMPERATURE_OFFSET;

            // Validate temperature is in reasonable range (25.0°C to 50.5°C)
            if (Temperature >= 25.0 || Temperature <= 50.5)
                return true;

            
            Temperature = 0;
            IsMalformed = true;
            return false;
            
        }
    }
}