using OpenTK.Graphics.OpenGL4;
using System.ComponentModel;

namespace Plotter.UserControls
{
    [ToolboxItem(false)]
    internal partial class MyPlotter : MyPlotterBase
    {
        protected Dictionary<string, MyPlot> Plots = [];
        public float TimeWindowSeconds { get; set; } = 10.0f;

        protected bool TestMode = false;
        protected string Debug = string.Empty;
        protected override void Init()
        {
            base.Init();

            if (!TestMode) return;

            sin = Plots["Sine Wave"]   = new MyPlot(1000, this);
            cos = Plots["Cosine Wave"] = new MyPlot(1000, this);
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

            // 4. Define the viewport based on the current time and the zoom window.
            float viewRight = _maxTime;
            float viewLeft = viewRight - TimeWindowSeconds;
            ViewPort = new RectangleF(viewLeft, -6, TimeWindowSeconds, 1030);

            int colorLocation = GL.GetUniformLocation(_plotShaderProgram, "uColor");
            foreach (var plot in Plots.Values)
            {
                GL.Uniform4(colorLocation, plot.Colour);

                plot.Render();
            }

            Debug = $"Plots: {Plots.Count}, Time: {_maxTime:F2}, Window: {TimeWindowSeconds}s";
        }

        protected override void DrawText()
            => fontRenderer?.RenderText(Debug, 10, 10);

    }
}