using OpenTK.Graphics.OpenGL4;
using System.ComponentModel;

namespace Plotter.UserControls
{
    [ToolboxItem(false)]
    internal partial class MyPlotter : MyPlotterBase
    {
        protected Dictionary<string, MyPlot> Plots = [];
        public float TimeWindowSeconds { get; set; } = 10.0f;

        private readonly RunningAverage _timeDeltaSmoother = new(15); // Window size of 15 frames
        private float _lastMaxTime = 0.0f;
        private float _smoothedViewRight = 0.0f;

        protected bool TestMode = false;
        protected string Debug = string.Empty;
        protected override void Init()
        {
            base.Init();
            MyGL.MouseWheel += MyGL_MouseWheel;
            if (!TestMode) return;

            sin = Plots["Sine Wave"]   = new MyPlot(1200, this);
            cos = Plots["Cosine Wave"] = new MyPlot(1200, this);
            Debug = "Test Mode: Sine and Cosine Waves";

        }
        MyPlot? sin;
        MyPlot? cos;


        private float _maxTime = 0.0f;
        protected override void DrawPlots()
        {
            if (Plots.Count == 0) return;

            if (TestMode)
            {
                sin?.Add(Math.Sin(sin.XCounter * 1.1) * 500 + 512);
                cos?.Add(Math.Cos(cos.XCounter * 1.1) * 500 + 512);
            }

            _maxTime = Math.Max(_maxTime, Plots.Values.Max(p => p.LastX));


            // Calculate the change in time since the last frame
            var timeDelta = _maxTime - _lastMaxTime;
            _lastMaxTime = _maxTime;

            // Return the delta to our smoother
            _timeDeltaSmoother.Add(timeDelta);

            // Advance the right edge of our viewport by the smoothed delta
            _smoothedViewRight += (float)_timeDeltaSmoother.Average;
            

            // Define the viewport based on the smoothed right edge
            float viewLeft = _smoothedViewRight - TimeWindowSeconds;
            ViewPort = new RectangleF(viewLeft, -6, TimeWindowSeconds, 1030);

            int colorLocation = GL.GetUniformLocation(_plotShaderProgram, "uColor");
            foreach (var plot in Plots.Values)
            {
                GL.Uniform4(colorLocation, plot.Colour);

                plot.Render();
            }

 //           Debug = $"Plots: {Plots.Count}, Time: {_maxTime:F2}, Window: {TimeWindowSeconds}s";
        }

        protected override void DrawText()
            => fontRenderer?.RenderText(Debug, 10, 10);

        private void MyGL_MouseWheel(object? sender, MouseEventArgs e)
        {
            const float zoomFactor = 1.1f;
            float newTimeWindow;

            if (e.Delta > 0)
                newTimeWindow = TimeWindowSeconds / zoomFactor;
            else
                newTimeWindow = TimeWindowSeconds * zoomFactor;

            newTimeWindow = Math.Clamp(newTimeWindow, 0.1f, 10.0f);

            GLThread.Enqueue(() => { TimeWindowSeconds = newTimeWindow; });
        }


    }
}