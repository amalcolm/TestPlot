using System;

namespace CTG_Comms
{
    public class CTGStatus
    {
        private readonly ushort _statusWord;
        private readonly ushort _hrMode;
        private readonly byte _tocoMode;

        public CTGStatus(C_Frame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            
            _statusWord = frame._statusWord;
            _hrMode = frame.HRmode;
            _tocoMode = frame.TOCOmode;
        }

        // Status Word Properties - High Byte
        public bool FMPEnabled => (_statusWord & 0x8000) != 0;
        public bool TwinOffsetActive => (_statusWord & 0x4000) != 0;
        public bool DECGLogicOn => (_statusWord & 0x1000) != 0;
        public bool CrossChannelVerificationDetected => (_statusWord & 0x0200) != 0;
        public bool TelemetryOn => (_statusWord & 0x0100) != 0;

        // Status Word Properties - Low Byte
        public bool FSpO2Available => (_statusWord & 0x0020) != 0;
        public bool CTGDataDeleted => (_statusWord & 0x0004) != 0;
        public bool DefaultCTGDataInserted => (_statusWord & 0x0002) != 0;
        public bool MonitorOn => (_statusWord & 0x0001) != 0;

        // HR Mode Properties
        public HRSourceMode HR1Mode => (HRSourceMode)((_hrMode >> 0) & 0x07);
        public HRSourceMode HR2Mode => (HRSourceMode)((_hrMode >> 4) & 0x07);
        public HRSourceMode MHRMode => (HRSourceMode)((_hrMode >> 8) & 0x07);
        public bool HR1Inop => (_hrMode & 0x0008) != 0;
        public bool HR2Inop => (_hrMode & 0x0080) != 0;
        public bool MHRInop => (_hrMode & 0x0800) != 0;

        // TOCO Mode Properties
        public TOCOSourceMode TOCOMode => (TOCOSourceMode)(_tocoMode & 0x07);

        public enum HRSourceMode
        {
            NoTransducer = 0,
            Ultrasound = 1,
            DECG = 2,
            MECG = 3,
            ExternalMHR = 4,
            Reserved1 = 5,
            Reserved2 = 6,
            Unknown = 7
        }

        public enum TOCOSourceMode
        {
            NoTransducer = 0,
            External = 4,
            IUP = 5,
            Unknown = 7
        }

        public override string ToString()
        {
            return $"CTG Status:\n" +
                   $"{"FMP:"             ,20} {(FMPEnabled ? "Enabled" : "Disabled")}\n" +
                   $"{"Twin Offset:"     ,20} {(TwinOffsetActive ? "Active" : "Inactive")}\n" +
                   $"{"DECG Logic:"      ,20} {(DECGLogicOn ? "On" : "Off")}\n" +
                   $"{"Cross Channel:"   ,20} {(CrossChannelVerificationDetected ? "Detected" : "Not Detected")}\n" +
                   $"{"Telemetry:"       ,20} {(TelemetryOn ? "On" : "Off")}\n" +
                   $"{"FSpO2:"           ,20} {(FSpO2Available ? "Available" : "Not Available")}\n" +
                   $"{"CTG Data:"        ,20} {(CTGDataDeleted ? "Deleted" : "Not Deleted")}\n" +
                   $"{"Default CTG Data:",20} {(DefaultCTGDataInserted ? "Inserted" : "Not Inserted")}\n" +
                   $"{"Monitor:"         ,20} {(MonitorOn ? "On" : "Off")}\n" +
                   $"{"HR1:"             ,20} {HR1Mode}{(HR1Inop ? " (Inop)" : "")}\n" +
                   $"{"HR2:"             ,20} {HR2Mode}{(HR2Inop ? " (Inop)" : "")}\n" +
                   $"{"MHR:"             ,20} {MHRMode}{(MHRInop ? " (Inop)" : "")}\n" +
                   $"{"TOCO:"            ,20} {TOCOMode}";
        }
    }
}