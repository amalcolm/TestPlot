using Plotter;
using System.IO.Ports;

namespace TestPlot
{
    partial class MainForm
    {

        private long _lastFrame = 0;

        private async void cbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPorts.SelectedItem is string data)
            {
                var io = myChart.IO;
                await io.SetPort(data);

                io.FrameReceived -= Io_OnFrame;
                io.DataReceived -= Io_OnData;
                io.Error -= Io_Error;
                if (io.isOpen)
                {
                    io.FrameReceived += Io_OnFrame;
                    io.DataReceived += Io_OnData;
                    io.Error += Io_Error;

                    io.Write("G");
                }
            }
        }

        private void FindSerial()
        {
            cbPorts.Items.Clear();
            cbPorts.SelectedIndex = -1;
            cbPorts.Text = string.Empty;

            for (bool firstRun = true; cbPorts.Items.Count == 0; firstRun = false)
            {
                DialogResult result =
                    firstRun ? DialogResult.Retry
                             : MessageBox.Show("Serial device could not be detected", "Serial Error",
                                    MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error);

                switch (result)
                {
                    case DialogResult.Abort:
                        this.Close();
                        return;

                    case DialogResult.Ignore:
                        this.Text = "Test Run - No Serial";
                        this.BackColor = Color.Magenta;
                        return;

                    case DialogResult.Retry:
                        var ports = SerialPort.GetPortNames();
                        if (ports.Length == 0) continue;

                        Array.Sort(ports);

                        cbPorts.Items.AddRange(ports);
                        break;
                }
            }

            if (cbPorts.Items.Count > 0)
                cbPorts.SelectedIndex = 0;
            else
                StartSerialMonitor();
        }

        private CancellationTokenSource? _serialMonitorCts = null;

        private void StartSerialMonitor()
        {
            _serialMonitorCts?.Cancel();
            _serialMonitorCts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                try
                {
                    // Get initial port list
                    var initialPorts = SerialPort.GetPortNames();
                    Array.Sort(initialPorts);

                    // Monitor until first change is detected or cancellation is requested
                    while (!_serialMonitorCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000, _serialMonitorCts.Token);

                        var currentPorts = SerialPort.GetPortNames();
                        Array.Sort(currentPorts);

                        // If ports have changed, process the change and exit the monitoring loop
                        if (initialPorts.SequenceEqual(currentPorts) == false)
                        {
                            var availablePorts = currentPorts
                                .OrderBy(p => p)
                                .ToArray();

                            // If no valid ports are found, restart monitoring
                            if (availablePorts.Length == 0)
                            {
                                initialPorts = currentPorts;
                                continue;
                            }

                            // Update UI on UI thread
                            this.Invoker(() =>
                            {
                                cbPorts.Items.Clear();
                                cbPorts.Items.AddRange(availablePorts);
                                cbPorts.SelectedIndex = 0;
                            });

                            // Exit the task - monitoring is complete
                            return;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Task was canceled, which is fine
                }
            });
        }

        private void Io_Error(object? sender, string Message)
        {
            _LOG_ERROR(Message);

            
            string Note = string.Empty;


            this.Invoker(() =>
            {

                tbComms.Text = Message; _oldText = string.Empty;

                if (MessageBox.Show(Note + Message, "Data Stream Error",
                                    MessageBoxButtons.OKCancel,
                                    MessageBoxIcon.Error) == DialogResult.Cancel)
                {
                    this.Close();
                    return;
                }

                FindSerial();
            });
        }

        Color _oldColor = Color.Empty;

        private string _oldText = string.Empty;

        private void Io_OnFrame(TestIO arg1, MyFrame frame)
        {
            _lastFrame = DateTime.Now.Ticks;

            if (isClosing) return;

            string output = "[unknown]";

            if (frame is C_Frame data)
            {
                // null output and reset colour
                output = string.Empty; if (_oldColor != Color.Empty)  { this.BackColor = _oldColor; _oldColor = Color.Empty; }

            }

            if (output == "[unknown]")
                output = frame.ToString();

            if (output != _oldText)
                try
                {
                    _oldText = output;
                    this.Invoker(() => tbComms.Text = output);
                }
                catch { _oldText = string.Empty; }
        }


        private void Io_OnData(TestIO testIO, TestIO.Packet packet)
        {
        }

    }
}
