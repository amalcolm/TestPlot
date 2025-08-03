namespace Plotter
{
    
    public class MyFrame : BaseFrame
    {

        public const int HeaderLen = 2;     // DLE:STX
        public const int FooterLen = 2 + 2; // DLE:ETX:CRC1:CRC2
        public enum StateKind { Empty, Start, DataBlock, ReadingCRC, Complete};
        
        public char Kind = '\0';
        
        public bool       ToWrite       { get; set; }
        public MyFrame?  NextFrame     { get; set; }

        public MyFrame() : base()
        {
            State       = StateKind.Empty;
            IsComplete  = false;
            IsMalformed = false;
            NextFrame   = null;
        }

        public MyFrame(bool toWrite) : this()
        {
            ToWrite     = toWrite;
        
            if (ToWrite)
            {
                Add(DLE);
                Add(STX);
            }
        }

        
        private uint StatusCount = 0;
        bool dleLast = false;
        public StateKind State { get => _State; set { _State = value; StatusCount = 0; } }
        private StateKind _State = StateKind.Empty;

        public void Add(byte b)
        {
            if (IsComplete) throw new InvalidOperationException("Calling Add on completed frame is not allowed");
            if (State == StateKind.Empty && bytes.Count == 0)
                State = StateKind.Start;

            bytes.Add(b);
            UpdateCRC(b);

            if (ToWrite == false)
            {
                StatusCount++;

                switch (State)
                {
                    case StateKind.Start:
                        switch (StatusCount)
                        {
                            case 1: if (b != DLE) throw new Exception("Header DLE expected"); else break;
                            case 2: if (b != STX) throw new Exception("Header STX expected"); else State = StateKind.DataBlock; break;
                        }
                        break;

                    case StateKind.DataBlock:
                        if (ReadyToSet)
                        {
                            Timestamp = DateTime.Now;
                            Kind = (char)b;
                        }
                        if (b == DLE) { dleLast = !dleLast; break; }
                        
                        if ( dleLast)
                             if (b == ETX) { State = StateKind.ReadingCRC; break;}
                        else if (b == STX) { IsMalformed = true;
                                             NextFrame = new(ToWrite);
                                             NextFrame.Add(DLE);
                                             NextFrame.Add(STX);
                                             return;
                                           }
                        else throw new Exception("Undoubled DLE");

                        break;

                    case StateKind.ReadingCRC:
                        if (StatusCount == 2)
                        {
                            State = StateKind.Complete;
                            IsComplete = true;
                            break;
                        };
                        break;
                }
            }
        }

        public DateTime Timestamp { get; private set; }

        public bool ReadyToSet => State == StateKind.DataBlock && StatusCount == 1;
        
        public override bool ProcessFrame()
        {
            if (base.ProcessFrame() == false || bytes.Count < 7) { IsMalformed = true; return false; }

            dataLen = bytes.Count - HeaderLen - FooterLen;

            data = bytes.Skip(HeaderLen).Take(dataLen).ToArray();
            dateIndex = 0;

            return true;
        }

        public byte[] ToArray()
        {
            if (bytes.Count == 0 || ToWrite == false) return [..bytes];

            Add(DLE);
            Add(ETX);

            return ToArrayCRC();
        }

        public override string ToString()
        {
            return string.Join(" ", bytes.Select(b => $"{b:X2}"));
        }

    }
}
