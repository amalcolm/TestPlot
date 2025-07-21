using System.Diagnostics;
using Color = System.Drawing.Color;
using Timer = System.Windows.Forms.Timer;

using ScottPlot;
using TestPlot;

namespace Plotter
{

    public partial class MyChart : UserControl
    {
        private readonly Plot? plot;

        private readonly Dictionary<int, MyPlot> dPlots = [];
        private readonly Timer tiRender = new() { Interval = 15 };

        public MyChart()
        {
            InitializeComponent();
            formView.Chart = this;

            plot = formsPlot.Plot;
            plot.DataBackground.Color = ScottPlot.Color.FromColor(formsPlot.BackColor);
//            plot.Axes.Bottom.TickGenerator = new TimeTickGenerator();

            tiRender.Tick += TiRender_Tick;
        }

        private void TiRender_Tick(object? sender, EventArgs e)
        {
            if (plot == null || !okayToRender || this.IsDisposed)
            {
                tiRender.Stop();
                return;
            }

            double time = frameTimer.Elapsed.TotalSeconds + offsetTime;
            plot.Axes.SetLimitsX(time - 9, time + 1);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Program.IsRunning)
                IO ??= new MySerialIO();
        }


        public MySerialIO IO
        {
            get => _io;
            set
            {
                if (_io == value) return;
                if (_io != null)
                {
                    _io.FrameReceived -= IO_FrameReceived;
                    _io.TextReceived  -= IO_TextReceived;
                }

                _io = value;

                if (_io != null)
                {
                    _io.FrameReceived += IO_FrameReceived;
                    _io.TextReceived  += IO_TextReceived;
                }
            }
        }

        private void IO_TextReceived(MySerialIO io, string text)
        {
            if (plot == null) return;
            CheckIsRunning();

            formView.Text = text;
        }

        private void IO_FrameReceived(MySerialIO io, MyFrame frame)
        {
            if (plot == null) return;
            CheckIsRunning();

            if (frame is C_Frame cFrame)
                AddData(cFrame);
        }

        private void CheckIsRunning()
        {

            if (okayToRender && frameTimer.IsRunning == false)
            {
                offsetTime = nPoints;
                frameTimer.Start();
                _ = nPoints;
                tiRender.Start();
            }
        }


        int nPoints = 0;
        double offsetTime = 0;


        public void AddData(C_Frame data)
        {
            for (int i = 0; i < C_Frame.SamplesPerFrame; i++)
            {
            }

            ///        var ra = dPlots[PlotKind.FHR].RunningAverage;
            ///        plot.Axes.SetLimitsY(ra.Min, ra.Max);

            nPoints++;
        }

        List<Color> plotColours = [
            Color.FromArgb(0x4E, 0x79, 0xA7), // Muted Blue
            Color.FromArgb(0xF2, 0x8E, 0x2B), // Orange
            Color.FromArgb(0xE1, 0x57, 0x59), // Red
            Color.FromArgb(0x76, 0xB7, 0xB2), // Teal
            Color.FromArgb(0x59, 0xA1, 0x4F), // Green
            Color.FromArgb(0xED, 0xC9, 0x48), // Yellow
            Color.FromArgb(0xB0, 0x7A, 0xA1), // Purple
            Color.FromArgb(0xFF, 0x9D, 0xA7), // Pink
            Color.FromArgb(0x9C, 0x75, 0x5F), // Brown
            Color.FromArgb(0xBA, 0xB0, 0xAC), // Grey
            Color.FromArgb(0x1F, 0x77, 0xB4), // Bright Blue
            Color.FromArgb(0xFF, 0x7F, 0x0E), // Bright Orange
            Color.FromArgb(0x2C, 0xA0, 0x2C), // Bright Green
            Color.FromArgb(0xD6, 0x27, 0x28), // Bright Red
            Color.FromArgb(0x94, 0x67, 0xBD), // Bright Purple
            Color.FromArgb(0x8C, 0x56, 0x4B), // Dark Brown
            Color.FromArgb(0xE3, 0x77, 0xC2), // Bright Pink
            Color.FromArgb(0x7F, 0x7F, 0x7F), // Medium Grey
            Color.FromArgb(0xBC, 0xBD, 0x22), // Olive Green
            Color.FromArgb(0x17, 0xBE, 0xCF)  // Cyan
        ];


        internal void AddData(string fieldName, int fieldHash, double v)
        {
            if (plot == null) return;

            if (dPlots.ContainsKey(fieldHash) == false)
            {
                var myPlot = new MyPlot(plot, fieldHash, plotColours[dPlots.Count]);
                dPlots.Add(fieldHash, myPlot);
            }

            dPlots[fieldHash].Add(v);
        }


        private MySerialIO _io = default!;
        private readonly Timer ti = new() { Interval = 1000 };
        private void MyChart_Load(object sender, EventArgs e)
        {
            ti.Tick += (s, e) => AddData(new C_Frame());

            okayToRender = true;

            if (IO?.isOpen == false)
                ti.Start();
        }

        bool okayToRender = false;
        protected override void OnHandleDestroyed(EventArgs e)
        {
            _cts.Cancel();
            okayToRender = false;
            tiRender.Stop();
            base.OnHandleDestroyed(e);
        }

        private readonly CancellationTokenSource _cts = new();

        private async Task StartAnimation()
        {
            try
            {
                if (plot == null || formsPlot == null)
                    return;

                while (!_cts.Token.IsCancellationRequested)
                {
                    double time = frameTimer.Elapsed.TotalSeconds + offsetTime;

                    plot.Axes.SetLimitsX(time - 9, time + 1);
                    this.Invoke(formsPlot.Refresh);
                    await Task.Delay(1, _cts.Token); // fast as possible, but let events through
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
            }
        }

        public void Clear()
        {
            frameTimer.Restart();

            foreach (var plot in dPlots.Values)
                plot.Restart();
        }


        private readonly Stopwatch frameTimer = new();


        private readonly ScottPlot.Color ColourNormal = ScottPlot.Color.FromColor(Color.Gainsboro);
        private readonly ScottPlot.Color ColourWarning = ScottPlot.Color.FromARGB(0xfff0a500);

        public bool IsWarning
        {

            get => plot?.DataBackground.Color == ColourWarning;
            set
            {
                if (plot != null)
                    plot.DataBackground.Color = (value) ? ColourWarning : ColourNormal;
            }
        }
    }
}