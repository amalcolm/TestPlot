using ScottPlot.Plottables;
using ScottPlot;


using Color = ScottPlot.Color;

namespace Plotter
{
 
    public class MyPlot
    {
        public Color Colour;
        public int Hash;
        public Signal Plot;

        public MySignalSource Data = new();
        public RunningAverage RunningAverage = new(40);

        public MyPlot(Plot ParentPlot, int hash, System.Drawing.Color colour)
        {
            Colour = Color.FromColor(colour);
            Hash = hash;

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
