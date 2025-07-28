using OpenTK.Graphics.OpenGL4;
using System.ComponentModel;

namespace Plotter.UserControls
{
    [ToolboxItem(false)]
    internal partial class MyPlotter : MyPlotterBase
    {
        protected Dictionary<string, MyPlot> Plots = [];

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


        /// <summary>
        /// This is our hook to set plot-specific properties right before rendering.
        /// It's called automatically by the base class's render loop for each plot.
        /// </summary>
        protected override void DrawPlots()
        {
            if (Plots.Count == 0) return;

            if (TestMode)
            {
                sin?.Add(Math.Sin(sin.XCounter * 1.1) * 500 + 512);
                cos?.Add(Math.Cos(cos.XCounter * 1.1) * 500 + 512);
            }

            int colorLocation = GL.GetUniformLocation(_plotShaderProgram, "uColor");

            float lastX = MyPlot.LastX;
            int windowSize = Plots.First().Value.WindowSize;

            ViewPort = new(lastX - windowSize, -6, windowSize, 1030);
            foreach (var plot in Plots.Values)
            {
                GL.Uniform4(colorLocation, plot.Colour);
                plot.Render();
            }

            Debug = $"Plots: {Plots.Count}, X: {lastX}, Window Size: {windowSize}";
        }
   
        protected override void DrawText()
            => fontRenderer?.RenderText(Debug, 10, 10);

    }
}