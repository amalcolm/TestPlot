using System.ComponentModel;

namespace Plotter.UserControls
{
    internal partial class MyChart : MyPlotter
    {
        private const int WindowSize = 1000;

        public MyChart()
            => InitializeComponent();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MySerialIO IO { get; set; } = default!;

        private void MyChart_Load(object sender, EventArgs e)
        {
            IO = new();
            IO.FrameReceived += IO_FrameReceived;
        }


        private void IO_FrameReceived(MySerialIO io, MyFrame frame)
        {
            if (TestMode) return;
            if (frame is not Text_Frame textFrame) return;

            var data = MyTextParser.Parse(textFrame.Text); if (data == null) return;

            foreach (var kvp in data)
            {
                if (Plots.TryGetValue(kvp.Key, out var plot) == false)
                {
                    plot = new MyPlot(WindowSize, this);
                    Plots[kvp.Key] = plot;
                }

                plot.Add(textFrame.Time, kvp.Value);
            }

        }



    }
}
