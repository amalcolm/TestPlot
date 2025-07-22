using OpenTK.Graphics.OpenGL4;

namespace Plotter
{
    internal partial class MyPlotter : MyPlotterBase
    {
        private FontFile? font;
        private FontRenderer? fontRenderer;

        protected Dictionary<string, MyPlot> Plots = [];

        System.Threading.Timer? timer;

        protected override void Init()
        {
            font = FontLoader.Load("Segoe UI.fnt");
            fontRenderer = new();

            var sin = Plots["Sine Wave"] = new MyPlot(2000, 1000);
            var cos = Plots["Cosine Wave"] = new MyPlot(2000, 1000);

            timer = new( (object? state) =>
            {
                if (IsDisposed || !IsLoaded) return;
                // Generate some sample data
                double time = DateTime.Now.TimeOfDay.TotalSeconds;
                sin.Add(Math.Sin(time));
                cos.Add(Math.Cos(time));
            
            }, null, 0, 10);
        }

        
        /// <summary>
        /// This is our hook to set plot-specific properties right before rendering.
        /// It's called automatically by the base class's render loop for each plot.
        /// </summary>
        protected override void DrawPlots()
        {
            int colorLocation = GL.GetUniformLocation(_plotShaderProgram, "uColor");
            ViewPort = new(-1, 1000, 1, -1); // Set viewport to full window
            foreach (var plot in Plots.Values)
            {
                GL.Uniform4(colorLocation, plot.Colour );
                plot.Render();
            }
        }
   
        protected override void DrawText()
        {   if (font == null) return;

            fontRenderer?.RenderText("Sine Wave", font, 10, 10);
        }

        protected override void ShutDown()
        {
        }
    }
}