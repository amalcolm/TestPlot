using ScottPlot.Plottables;
using ScottPlot;


using Color = ScottPlot.Color;

namespace CTG_Comms
{
    public enum CTG_PlotKind { FHR, MHR, SpO2, TOCO }

    public class CTG_Plot
    {
        public Color Colour;
        public CTG_PlotKind Kind;
        public Signal Plot;

        public MySignalSource Data = new();
        public RunningAverage RunningAverage = new(40);
        public CTG_Plot(Plot ParentPlot, CTG_PlotKind kind, System.Drawing.Color colour)
        {
            Colour = Color.FromColor(colour);
            Kind = kind;
            Plot = ParentPlot.Add.Signal(Data, Colour);
       }

        public void Add(double y)
        {
            double val = (y == 0 ? double.NaN : y);

            Data.Add(val);
            RunningAverage.Add(val);
        }

        public void Restart()
        {
            Data.Clear();
            RunningAverage.Reset();
        }
    }
}
