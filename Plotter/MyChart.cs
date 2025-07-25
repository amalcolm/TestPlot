﻿using System.ComponentModel;

namespace Plotter
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
            IO.TextReceived += IO_TextReceived;
        }


        private void IO_TextReceived(MySerialIO io, string text)
        {
            var data = MyTextParser.Parse(text); if (data == null) return;

            lock (_lock)
            {
                foreach (var kvp in data)
                {
                    if (Plots.TryGetValue(kvp.Key, out var plot))
                    {
                        plot.Add(kvp.Value);
                    }
                    else
                    {
                        // Create a new plot if it doesn't exist
                        var newPlot = new MyPlot(WindowSize);
                        newPlot.Add(kvp.Value);
                        Plots[kvp.Key] = newPlot;
                    }
                }
            }

        }



    }
}
