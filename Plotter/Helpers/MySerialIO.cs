using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Plotter
{
    public partial class MySerialIO
    {
        public enum IOMode { Raw, Text, Frames }
        public IOMode Mode { get; set; } = IOMode.Raw;

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

        public delegate void TextHandler(MySerialIO io, string text);
        public delegate void FrameHandler(MySerialIO io, MyFrame frame);
        public delegate void DataHandler(MySerialIO io, Packet packet);

        public event TextHandler? TextReceived;
        public event FrameHandler? FrameReceived;
        public event DataHandler? DataReceived;

        public event EventHandler<string>? Error;

        private SerialPort SP { get; set; } = default!;
        private CancellationTokenSource? readCancellation;

        public bool isOpen { get; private set; } = false;
        public bool FindngStart = true;


        public MySerialIO()
        {
            if (SocketWatcher.IO != null) throw new InvalidOperationException("SocketWatcher.IO is already set. Please close the existing connection first.");

            SocketWatcher.IO = this;
            SocketWatcher.StartListening();
        }

        public async Task<bool> SetPort(string port)
        {
            try
            {
                if (SP != null && SP.IsOpen)
                    await Close();

                SP = new SerialPort(port)
                {
                    BaudRate = 1200,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    WriteTimeout = 1500,
                    ReadTimeout = 1500
                };

                Connect();

                return isOpen;
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex.Message);
                return false;
            }

        }

        public void Connect()
        {
            if (isOpen) return;

            SP.Open();

            isOpen = SP.IsOpen;

            if (isOpen)
            {
                readCancellation = new();
                _ = StartReading(readCancellation.Token);
            }
        }


        MyFrame InFrame = new(toWrite: false);
        MyFrame OutFrame = new(toWrite: true);
        bool IsContinuous = false;
        int nLastDataTick = 0;

        private async Task StartReading(CancellationToken cancellationToken)
        {
            Debug.WriteLine("Reading task started.");

            // discard buffered data and write handshake
            _ = SP.ReadExisting();
            SP.Write(HS_Plotter, 0, HS_Plotter.Length);

            nLastDataTick = Environment.TickCount;
            DateTime? packetStartTime = null;
            List<byte> currentPacket = [];
            FindngStart = true;
            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(4096);

            while (SP?.IsOpen == true && !cancellationToken.IsCancellationRequested && isOpen)
            {
                try
                {
                    int size = SP.BytesToRead;

                    if (size > 0)
                    {
                        packetStartTime ??= DateTime.Now;

                        // Make sure we don't read more than our buffer can hold
                        int bytesToRead = Math.Min(size, rentedBuffer.Length);
                        int bytesRead = SP.Read(rentedBuffer, 0, bytesToRead);

                        // Add the segment of the buffer that contains new data
                        currentPacket.AddRange(rentedBuffer.Take(bytesRead)); // Still allocates, but let's fix the next part first

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
                catch (TaskCanceledException)
                {
                    break;
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
                finally
                {
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                }
            }

            isOpen = SP != null && SP.IsOpen;

            if (cancellationToken.IsCancellationRequested == false)
                Error?.Invoke(this, ERROR_DISCONNECTED);

            Debug.WriteLine("Reading task exitted.");

        }

        private int handshakeMatchIndex = 0;
        private int consecutiveTextPackets = 0;
        private static readonly int TEXT_PROBE_THRESHOLD = 0;
        private static readonly byte[] HS_fNIRS_Probe = { 0x10, 0x02, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00 };
        private static readonly byte[] HS_Plotter = { 0x10, 0x02, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x01 };


        private void ProcessData(DateTime timestamp, byte[] bytes)
        {
            switch (Mode)
            {
                case IOMode.Text: ProcessTextData(bytes); return;
                case IOMode.Frames: ProcessFrameData(bytes); return;
            }

            DataReceived?.Invoke(this, (timestamp, bytes));

            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == HS_fNIRS_Probe[handshakeMatchIndex])
                {
                    if (++handshakeMatchIndex == HS_fNIRS_Probe.Length)
                    {
                        Mode = IOMode.Frames;
                        handshakeMatchIndex = 0;
                        return;
                    }
                }
                else
                {
                    if (handshakeMatchIndex > 0)
                    {
                        handshakeMatchIndex = 0;
                        i--;
                    }
                }
            }

            if (Mode == IOMode.Raw)
            {
                bool looksLikeText = bytes.All(b => b == '\n' || b == '\r' || b == '\t' || (b >= 32 && b < 127));

                if (looksLikeText)
                    consecutiveTextPackets++;
                else
                    consecutiveTextPackets = 0;

                if (consecutiveTextPackets >= TEXT_PROBE_THRESHOLD)
                {
                    Mode = IOMode.Text;
                    ProcessData(timestamp, bytes);
                }
            }
        }

        // Use a List<byte> as a class-level buffer.

        private readonly List<byte> byteBuffer = [];

        private void ProcessTextData(byte[] bytes)
        {
            byteBuffer.AddRange(bytes);

            while (true)
            {
                // Get a Span that views the list's memory directly. No copy.
                var bufferSpan = CollectionsMarshal.AsSpan(byteBuffer);
                int newlineIndex = bufferSpan.IndexOf((byte)'\n');

                if (newlineIndex < 0) break;

                int lineLength = newlineIndex;
                if (lineLength > 0 && bufferSpan[lineLength - 1] == (byte)'\r')
                {
                    lineLength--;
                }

                // Get a slice of the span representing just the line. No copy.
                var lineSpan = bufferSpan.Slice(0, lineLength);

                // Directly create a string from the span.
                // This is the *only allocation* left in the loop.
                TextReceived?.Invoke(this, Encoding.UTF8.GetString(lineSpan));

                // Efficiently remove the processed part of the list.
                byteBuffer.RemoveRange(0, newlineIndex + 1);
            }
        }
        public void ProcessFrameData(byte[] bytes)
        {
            if (FindngStart)
                for (int i = 0; i < bytes.Length - 1; i++)
                    if (bytes[i] == BaseFrame.DLE && bytes[i + 1] == BaseFrame.STX)
                    {
                        bytes = [.. bytes.Skip(i)];
                        FindngStart = false;
                        break;
                    }


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
                case "G": IsContinuous = true; break;
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
                    isOpen = false;  // Force state to closed regardless of what SP.ShutDown did
                }
            }
        }

        /// <summary>
        /// Gets a list of all present USB serial devices that match the known vendor IDs.
        /// The list is sorted numerically by COM port number.
        /// </summary>
        /// <returns>A sorted array of COM port names (e.g., "COM1", "COM9").</returns>
        public static string[] GetUSBSerialPorts()
        {
            List<string> ports = [];

            // Query for all devices in the "Ports" class to narrow down the search.
            string searchQuery = "SELECT * FROM Win32_PnPEntity WHERE ClassGuid = '{4d36e978-e325-11ce-bfc1-08002be10318}'";

            try
            {
                using (var searcher = new ManagementObjectSearcher(searchQuery))
                {
                    foreach (var device in searcher.Get())
                    {
                        string deviceId = device["DeviceID"]?.ToString() ?? string.Empty;
                        string deviceName = device["Name"]?.ToString() ?? string.Empty;

                        // Check if the device's Vendor ID is in our list of known serial vendors.
                        bool isKnownVendor = UsbSerialVendorIds.Keys.Any(vid => deviceId.Contains($"VID_{vid}", StringComparison.OrdinalIgnoreCase));

                        if (isKnownVendor)
                        {
                            // Extract the COM port from the device name.
                            Match match = ComPortRegex().Match(deviceName);
                            if (match.Success)
                            {
                                ports.Add(match.Value.Trim('(', ')'));
                            }
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                Console.WriteLine($"An error occurred while querying WMI: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }

            // Sort the found ports using the natural string comparer.
            ports.Sort(new NaturalStringComparer());

            return [.. ports];
        }
        [GeneratedRegex(@"\(COM\d+\)")] private static partial Regex ComPortRegex();


        public static readonly Dictionary<string, string> UsbSerialVendorIds = new()
        {
            // Tier 1: The "Big Four" Dedicated Chip Makers
            // These companies are the most common manufacturers of dedicated USB-to-Serial bridge ICs.
            { "0403", "FTDI (Future Technology Devices International)" },
            { "067B", "Prolific Technology Inc." },
            { "1A86", "WCH (QinHeng Electronics)" },
            { "10C4", "Silicon Labs" },

            // Tier 2: The Microcontroller & Platform Vendors
            // These companies manufacture microcontrollers that often include native USB-CDC (serial) capabilities.
            { "16C0", "V-USB / PJRC (Teensy)" }, // Generic VID for V-USB stack, famously used by Teensy.
            { "2341", "Arduino" },
            { "2E8A", "Raspberry Pi" }, // Specifically for the RP2040 chip (Pi Pico).
            { "0483", "STMicroelectronics" }, // For STM32 microcontrollers.
            { "04D8", "Microchip Technology" }, // Includes Atmel products.

            // Tier 3: The Hobbyist & Niche Vendors
            // These are popular suppliers in the maker community who often register their own VIDs for custom boards.
 //           { "1B4F", "SparkFun Electronics" },
 //           { "239A", "Adafruit Industries" }
        };



        [ToolboxItem(false)]
        public partial class NaturalStringComparer : IComparer<string>
        {
            public int Compare(string? x, string? y)
            {
                var regex = Regex_FindNumber();

                var matchX = regex.Match(x ?? string.Empty);
                var matchY = regex.Match(y ?? string.Empty);

                if (matchX.Success && matchY.Success)
                    if (int.TryParse(matchX.Value, out int numX) && int.TryParse(matchY.Value, out int numY))
                    {
                        int numComparison = numX.CompareTo(numY);
                        if (numComparison != 0)
                            return numComparison;
                    }
    
                // Fallback to regular string comparison if numbers are the same or not found
                return string.Compare(x, y, StringComparison.Ordinal);
            }
        }
        [GeneratedRegex("(\\d+)")] private static partial Regex Regex_FindNumber();

    }
}
