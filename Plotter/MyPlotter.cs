using OpenTK.Graphics.OpenGL4;
using System.ComponentModel;

namespace Plotter
{
    [ToolboxItem(false)]
    internal partial class MyPlotter : MyPlotterBase
    {
        protected readonly object _lock = new();
        protected FontFile? font;
        protected FontRenderer? fontRenderer;

        protected bool TestMode = false;
        protected Dictionary<string, MyPlot> Plots = [];

        System.Threading.Timer? timer;

        protected override void Init()
        {
            font = FontLoader.Load("Roboto-Medium.json");
            fontRenderer = new();

            if (!TestMode) return;

            var sin = Plots["Sine Wave"]   = new MyPlot(1000);
            var cos = Plots["Cosine Wave"] = new MyPlot(1000);

            timer = new( (object? state) =>
            {
                lock (_lock)
                {
                    if (IsDisposed || !IsLoaded) return;
                    // Generate some sample data
                    double time = DateTime.Now.TimeOfDay.TotalSeconds;
                    sin.Add(Math.Sin(time));
                    cos.Add(Math.Cos(time));
                }            
            }, null, 0, 10);
        }


        /// <summary>
        /// This is our hook to set plot-specific properties right before rendering.
        /// It's called automatically by the base class's render loop for each plot.
        /// </summary>
        protected override void DrawPlots()
        {
            if (Plots.Count == 0) return;
            int colorLocation = GL.GetUniformLocation(_plotShaderProgram, "uColor");

            lock (_lock)
            {
                float lastX = (float)Plots.First().Value.XCounter;
                int windowSize = Plots.First().Value.WindowSize;

                ViewPort = new(lastX - windowSize, -6, windowSize, 1030);
                foreach (var plot in Plots.Values)
                {
                    GL.Uniform4(colorLocation, plot.Colour);
                    plot.Render();
                }
            }
        }
   
        protected override void DrawText()
        {   if (font == null) return;

            if (TestMode)
                fontRenderer?.RenderText("Sine Wave", font, 10, 10);
        }

        protected override void ShutDown()
        {
        }
    }
}