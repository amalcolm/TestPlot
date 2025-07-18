using System.Diagnostics;
using Color = System.Drawing.Color;

using ScottPlot;
using ScottPlot.WinForms;
using TestPlot;

namespace Plotter;

public partial class MyChart : UserControl
{
    private readonly Plot? plot;

    private readonly Dictionary<PlotKind, MyPlot> dPlots = [];



    public TestIO IO
    {
        get => _io;
        set
        {
            _io = value;
            if (_io == null)
                return;

            IO.FrameReceived += IO_DataReceived;
        }
    }

    private void IO_DataReceived(TestIO testIO, MyFrame frame)
    {
        if (frame is C_Frame cFrame)
            AddData(cFrame);
    }

    public MyChart()
    {
        InitializeComponent();

        
        plot = formsPlot1.Plot;
        plot.DataBackground.Color = ScottPlot.Color.FromColor(formsPlot1.BackColor); 

        var plots = new List<MyPlot>
        {
            new(plot, PlotKind.FHR , Color.Red  ),
            new(plot, PlotKind.MHR , Color.Green),
            new(plot, PlotKind.SpO2, Color.Blue ),
            new(plot, PlotKind.TOCO, Color.Black),
        };

        dPlots = plots.ToDictionary(p => p.Kind, p => p);

        plot.Axes.Bottom.TickGenerator = new TimeTickGenerator();

    }

    int nPoints = 0;
    double offsetTime = 0;


    public void AddData(C_Frame data)
    {
        if (plot == null || formsPlot1 == null)
            return;

        if (okayToRender && frameTimer.IsRunning == false)
        {
            offsetTime = nPoints;
            frameTimer.Start();
            _ = nPoints;
            _ = StartAnimation();
        }

        for (int i = 0; i < C_Frame.SamplesPerFrame; i++)
        {
            dPlots[PlotKind.SpO2].Add(data.SpO2);
        }

        var ra = dPlots[PlotKind.FHR].RunningAverage;
        plot.Axes.SetLimitsY(ra.Min, ra.Max);

        nPoints++;
    }

    private TestIO _io = new();
    private readonly System.Windows.Forms.Timer ti = new() { Interval = 1000 };
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
        base.OnHandleDestroyed(e);
    }

    private readonly CancellationTokenSource _cts = new();

    private async Task StartAnimation()
    {
        try
        {
            if (plot == null || formsPlot1 == null)
                return;

            while (!_cts.Token.IsCancellationRequested)
            {
                double time = frameTimer.Elapsed.TotalSeconds + offsetTime;

                plot.Axes.SetLimitsX(time - 9, time + 1);
                this.Invoke(formsPlot1.Refresh);
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

        get => plot?.DataBackground.Color == ColourWarning;
        set
        {
            if (plot != null)
                plot.DataBackground.Color = (value) ? ColourWarning : ColourNormal;
        }
    }
}
