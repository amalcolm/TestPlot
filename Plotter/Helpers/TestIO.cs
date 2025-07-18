using System.IO.Ports;
using System.Text;

namespace Plotter
{
    public class TestIO
    {
        public const string ERROR_TIMEOUT = "!Data from CTG has stopped";
        public const string ERROR_DISCONNECTED = "!CTG has disconnected";
        public const string ERROR_FRAME_NOT_RECOGNISED = "!Frame not recognised";
        public struct Packet
        {
            public DateTime Timestamp;
            public byte[] Data;

            public static implicit operator Packet((DateTime timestamp, byte[] data) tuple)
                => new() { Timestamp = tuple.timestamp, Data = tuple.data };

            public static implicit operator (DateTime timestamp, byte[] data)(Packet packet)
                => (packet.Timestamp, packet.Data);
        }

        public delegate void FrameHandler(TestIO testIO, MyFrame frame);
        public delegate void DataHandler(TestIO testIO, Packet packet);

        public event FrameHandler? FrameReceived;
        public event DataHandler? DataReceived;

        public event EventHandler<string>? Error;

        private SerialPort SP { get; set; } = default!;
        private CancellationTokenSource? readCancellation;

        public bool isOpen { get; private set; } = false;
        public bool FindngStart = true;

        public async Task<bool> SetPort(string port)
        {
            try
            {
                if (SP != null && SP.IsOpen)
                    await Close();

                SP = new SerialPort(port)
                {
                    BaudRate     = 1200,
                    DataBits     = 8,
                    StopBits     = StopBits.One,
                    Parity       = Parity.None,
                    WriteTimeout = 1500,
                    ReadTimeout  = 1500
                };

                SP.Open();

                isOpen = SP.IsOpen;

                if (isOpen)
                {
                    readCancellation = new();
                    _ = StartReading(readCancellation.Token);
                }


                return isOpen;
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex.Message);
                return false;
            }

        }

        MyFrame InFrame  = new(toWrite: false);
        MyFrame OutFrame = new(toWrite: true);
        bool IsContinuous = false;
        int nLastDataTick = 0;


        private async Task StartReading(CancellationToken cancellationToken)
        {
            // discard buffered data
            _ = SP.ReadExisting();

            nLastDataTick = Environment.TickCount;
            DateTime? packetStartTime = null;
            List<byte> currentPacket = [];
            FindngStart = true;

            while (SP?.IsOpen == true && !cancellationToken.IsCancellationRequested && isOpen)
            {
                try
                {
                    int size = SP.BytesToRead;

                    if (size > 0)
                    {
                        packetStartTime ??= DateTime.Now;

                        byte[] tempBytes = new byte[size];
                        int bytesRead = SP.Read(tempBytes, 0, size);
                        currentPacket.AddRange(tempBytes.Take(bytesRead));

                        // No need for a delay here - just loop immediately
                        nLastDataTick = Environment.TickCount;
                        continue;
                    }

                    // If we have data but see no new bytes, we've found a gap
                    if (currentPacket.Count > 0 && packetStartTime.HasValue)
                    {
                        ProcessData(packetStartTime.Value, [.. currentPacket]);

                        currentPacket.Clear();
                        packetStartTime = null;
                    }

                    if (IsContinuous && (Environment.TickCount - nLastDataTick > 2000))
                    {
                        string message = ERROR_TIMEOUT;
                        nLastDataTick = Environment.TickCount; // avoid avalanche of messages

                        try
                        {
                            if (SP.IsOpen)
                            {
                                SP.DiscardOutBuffer();
                            }
                        }
                        catch (Exception ex)
                        {
                            message += $"\r\n  System Error:\r\n\r\n\t{ex.Message}";
                        }

                        throw new Exception(message);

                    }

                    // Wait one system tick ; '1' being less than the tick length
                    await Task.Delay(1, cancellationToken);
                }
                catch (Exception ex)
                {
                    var header = ex.Message.StartsWith("!") ? string.Empty : $"!Error reading from CTG: ";
                    var message = $"{header}{ex.Message}";
                    Error?.Invoke(this, message);

                    if (ex.Message == ERROR_TIMEOUT)
                        continue;
                    
                    if (ex.Message == ERROR_FRAME_NOT_RECOGNISED)
                    {
                        currentPacket.Clear();
                        SP.DiscardInBuffer();
                        continue;
                    }

                    await Task.Delay(100, cancellationToken);
                    readCancellation?.Cancel();

                    if (SP.IsOpen)
                        SP.Close();
                 
                    break;
                }
            }

            isOpen = (SP == null) ? false : SP.IsOpen;
    
            if (cancellationToken.IsCancellationRequested == false)
                Error?.Invoke(this, ERROR_DISCONNECTED);
        }

        private void ProcessData(DateTime timestamp, byte[] bytes)
        {
            DataReceived?.Invoke(this, (timestamp, bytes));

            if (FindngStart)
                for (int i = 0; i < bytes.Length - 1; i++)
                    if (bytes[i] == BaseFrame.DLE && bytes[i + 1] == BaseFrame.STX)
                    {
                        bytes = [.. bytes.Skip(i)];
                        FindngStart = false;
                        break;
                    }

            if (FindngStart) return;
            
            foreach (var b in bytes)
            {
                InFrame.Add(b);

                if (InFrame.ReadyToSet)
                    try
                    {
                        InFrame = InFrame.SetType() ?? InFrame;
                    }
                    catch
                    {
                        InFrame = new(toWrite: false);
                        FindngStart = true;
                        throw;
                    }


                if (InFrame.IsComplete)
                {
                    InFrame.ProcessFrame();
                    FrameReceived?.Invoke(this, InFrame);
                }

                if (InFrame.IsMalformed || InFrame.IsComplete)
                    InFrame = InFrame.NextFrame ?? new MyFrame(InFrame.ToWrite);
            }
        }

        public byte[] Write(string val)
        {
            OutFrame = new(toWrite: true);

            switch (val)
            {
                case "G": IsContinuous = true ; break;
                case "H": IsContinuous = false; break;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(val);
            foreach (var b in bytes)
                OutFrame.Add(b);

            byte[] toWrite = OutFrame.ToArray();

            if (isOpen)
            {
                if (SP.IsOpen)
                    SP.Write(toWrite, 0, toWrite.Length);
                return bytes;
            }

            return [];
        }



        public async Task Close()
        {
            if (isOpen)
            {
                readCancellation?.Cancel();
                await Task.Delay(500).ConfigureAwait(false);

                try
                {
                    using var cts = new CancellationTokenSource(1000);
                    await Task.Run(() => { if (SP.IsOpen) SP.Close(); }, cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Close error: {ex.Message}");
                }
                finally
                {
                    isOpen = false;  // Force state to closed regardless of what SP.Close did
                }
            }
        }

    }
}
