using System.Diagnostics;
using Color = System.Drawing.Color;

using ScottPlot;
using ScottPlot.WinForms;


namespace CTG_Comms
{
    public partial class CTG_Chart : UserControl
    {
        private readonly FormsPlot formsPlot;
        private readonly Plot      plot;

        private readonly Dictionary<CTG_PlotKind, CTG_Plot> dPlots = [];
        
   

        public TestIO IO {
            get => _io;
            set
            {
                _io = value;
                IO.FrameReceived += IO_DataReceived;
            }
        }

        private void IO_DataReceived(TestIO testIO, CTGframe frame)
        {
            if (frame is C_Frame cFrame)
                AddData(cFrame);
        }

        public CTG_Chart()
        {
            InitializeComponent();

            formsPlot = new FormsPlot
            {
                Dock = DockStyle.Fill,
                Parent = this,
                BackColor = Color.FromArgb(53, 53, 53)
            };
            plot = formsPlot.Plot;
            plot.DataBackground.Color = ColourNormal;

            var plots = new List<CTG_Plot>
            {
                new(plot, CTG_PlotKind.FHR , Color.Red  ),
                new(plot, CTG_PlotKind.MHR , Color.Green),
                new(plot, CTG_PlotKind.SpO2, Color.Blue ),
                new(plot, CTG_PlotKind.TOCO, Color.Black),
            };
            
            dPlots = plots.ToDictionary(p => p.Kind, p => p);

            plot.Axes.Bottom.TickGenerator = new TimeTickGenerator();

            this.Controls.Add(formsPlot);

        }

        int nPoints = 0;
        double offsetTime = 0;


        public void AddData(C_Frame data)
        {
            if (okayToRender && frameTimer.IsRunning == false)
            {
                offsetTime = nPoints;
                frameTimer.Start();
                _ = nPoints;
                _ = StartAnimation();
            }

            for (int i = 0; i < C_Frame.SamplesPerFrame; i++)
            {
                dPlots[CTG_PlotKind.FHR ].Add(data.HR1 [i]);
                dPlots[CTG_PlotKind.MHR ].Add(data.MHR [i]);
                dPlots[CTG_PlotKind.SpO2].Add(data.SpO2   );
                dPlots[CTG_PlotKind.TOCO].Add(data.TOCO[i]);
            }

            var ra = dPlots[CTG_PlotKind.FHR].RunningAverage;
            plot.Axes.SetLimitsY(ra.Min, ra.Max);

            nPoints++;
        }

        private TestIO _io = default!;
        private readonly System.Windows.Forms.Timer ti = new() { Interval = 1000 };
        private void CTG_Chart_Load(object sender, EventArgs e)
        {
            ti.Tick += (s, e) =>  AddData(new C_Frame());
            
            okayToRender = true;

            if (IO.isOpen == false)
                ti.Start();
        }

        bool okayToRender = false;
        protected override void OnHandleDestroyed(EventArgs e)
        {
            _cts.Cancel();
            okayToRender = false;
            base.OnHandleDestroyed(e);
        }

        private readonly CancellationTokenSource _cts = new();

        private async Task StartAnimation()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    double time = frameTimer.Elapsed.TotalSeconds + offsetTime;

                    plot.Axes.SetLimitsX(time - 9, time + 1);
                    this.Invoke(formsPlot.Refresh);
                    await Task.Delay(33, _cts.Token); // ~30fps
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


        private readonly ScottPlot.Color ColourNormal = ScottPlot.Color.FromARGB(0xff353535);
        private readonly ScottPlot.Color ColourWarning = ScottPlot.Color.FromARGB(0xfff0a500);

        public bool IsWarning
        {

            get => plot.DataBackground.Color == ColourWarning;
            set
            {
                plot.DataBackground.Color = (value) ? ColourWarning : ColourNormal;
            }
        }
    }
}
