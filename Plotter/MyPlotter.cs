using OpenTK.Graphics.OpenGL4;
using Timer = System.Windows.Forms.Timer;

namespace Plotter
{

    public partial class MyPlotter : MyPlotterBase
    {
        private double _animationTime = 0;

        private readonly Timer animationTimer = new() { Interval = 16 }; // ~60 FPS
        /// <summary>
        /// This is where we create the specific plots we want to display.
        /// It's called automatically by the base class during initialization.
        /// </summary>
        protected override void CreatePlots()
        {
            // Create two plot objects with a buffer for 1000 vertices each
            var sinePlot = new MyGLPlot(maxVertices: 1000, windowSize: 1000);
            var cosinePlot = new MyGLPlot(maxVertices: 1000, windowSize: 1000);

            // Add them to the base class's render engine with unique keys
            AddPlot("sine_wave", sinePlot);
            AddPlot("cosine_wave", cosinePlot);

            this.LeadPlotKey = "sine_wave";

            // Start the animation timer
            animationTimer.Tick += UpdatePlotData;
            animationTimer.Start();
        }

        /// <summary>
        /// This method is called repeatedly to add new data points and create animation.
        /// </summary>
        private void UpdatePlotData(object? sender, System.EventArgs e)
        {
            // Get the plots by their keys
            var sinePlot = GetPlot("sine_wave");
            var cosinePlot = GetPlot("cosine_wave");

            if (sinePlot == null || cosinePlot == null) return;

            // Advance the animation and add new data
            _animationTime += 0.05;
            sinePlot.Add(Math.Sin(_animationTime) * 0.5); // Scale to fit nicely
            cosinePlot.Add(Math.Cos(_animationTime) * 0.5);
        }

        /// <summary>
        /// This is our hook to set plot-specific properties right before rendering.
        /// It's called automatically by the base class's render loop for each plot.
        /// </summary>
        protected override void OnRenderPlot(MyGLPlot plot)
        {
            int colorLocation = GL.GetUniformLocation(_shaderProgram, "uColor");

            // Check which plot is being rendered and set its color
            if (plot == GetPlot("sine_wave"))
            {
                GL.Uniform4(colorLocation, Color.DeepSkyBlue);
            }
            else if (plot == GetPlot("cosine_wave"))
            {
                GL.Uniform4(colorLocation, Color.OrangeRed);
            }
        }
    }
}