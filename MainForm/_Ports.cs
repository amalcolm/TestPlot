using Plotter;

namespace TestPlot
{
    partial class MainForm
    {


        private async void cbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPorts.SelectedItem is string data)
            {
                var io = myChart.IO;        if (io == null) { _LOG_ERROR("No IO instance available"); return; }

                await io.SetPort(data);

                io.DataReceived -= Io_OnData;
                if (io.isOpen)
                    io.DataReceived += Io_OnData;
          }
        }

        private void FindSerial()
        {
            if (!Program.IsRunning || isClosing) return;

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
                        var ports = MySerialIO.GetUSBSerialPorts();
                        if (ports.Length == 0) continue;

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
                    var storedPorts = MySerialIO.GetUSBSerialPorts();

                    // Monitor until first change is detected or cancellation is requested
                    while (!_serialMonitorCts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000, _serialMonitorCts.Token);

                        var currentPorts = MySerialIO.GetUSBSerialPorts();
               
                        // If ports have changed, process the change and exit the monitoring loop
                        if (storedPorts.SequenceEqual(currentPorts) == false)
                        {
                           storedPorts = currentPorts;
                         
                            // Update UI on UI thread
                            this.Invoker(() =>
                            {
                                cbPorts.Items.Clear();
                                if (currentPorts.Length == 0)
                                {
                                    cbPorts.Text = "No Serial Ports Found";
                                    cbPorts.SelectedIndex = -1;
                                    return;
                                }

                                cbPorts.Items.AddRange(currentPorts);
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

        private void Io_OnData(MySerialIO io, MySerialIO.Packet packet)
        {
            System.Diagnostics.Debug.WriteLine(packet.Data.Length);
        }

    }
}
