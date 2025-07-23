using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Timer = System.Windows.Forms.Timer;

namespace Plotter
{
    internal abstract class MyPlotterBase : UserControl
    {
        private readonly GLControl _glControl;
        private readonly Timer _renderTimer;

        public RectangleF ViewPort { get; set; } = new(0, 1, 100, 2);

        // Shader programs
        protected int _plotShaderProgram;
        protected int _textShaderProgram;

        public bool IsLoaded => _isLoaded;
        private bool _isLoaded = false;

        protected MyPlotterBase()
        {
            // Basic control setup
            var mode = new OpenTK.Graphics.GraphicsMode(32, 24, 0, 4); // 32-bit color, 24-bit depth, 0 stencil, 4 samples for MSAA
            _glControl = new(mode) { Dock = DockStyle.Fill };
            this.Controls.Add(_glControl);

            // Hook the one-time load event
            _glControl.Load += OnLoad;
            _glControl.Resize += OnResize;

            _renderTimer = new Timer() { Interval = 15 };
            _renderTimer.Tick += Render;
        }

        /// <summary>
        /// Final setup method that initializes OpenGL, shaders, and plots.
        /// This is sealed to ensure the base setup logic cannot be bypassed.
        /// </summary>
        private void OnLoad(object? sender, EventArgs e)
        {
            if (IsLoaded || IsDisposed) return;

            GL.Viewport(0, 0, _glControl.ClientSize.Width, _glControl.ClientSize.Height);

            GL.ClearColor(Color.Gainsboro);
            // Enable blending for transparency
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _plotShaderProgram = ShaderManager.Get("plot");
            _textShaderProgram = ShaderManager.Get("font");
            Init();
            _isLoaded = true;  // allow rendering to start
            _renderTimer.Start();
        }

        private void OnResize(object? sender, EventArgs e)
        {
            if (!_isLoaded) return;
            // Update the viewport to match the new control size
            GL.Viewport(0, 0, _glControl.ClientSize.Width, _glControl.ClientSize.Height);
        }

        /// <summary>
        /// The main render loop. Renders all registered plots.
        /// </summary>
        private void Render(object? sender, EventArgs e)
        {
            if (!IsLoaded || IsDisposed) return;

            _glControl.MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(_plotShaderProgram);

            // --- Calculate and Upload Transformation Matrix ---
            var transform = Matrix4.CreateOrthographicOffCenter(ViewPort.Left, ViewPort.Right, ViewPort.Bottom, ViewPort.Top, -1.0f, 1.0f);
            int transformLocation = GL.GetUniformLocation(_plotShaderProgram, "uTransform");
            GL.UniformMatrix4(transformLocation, false, ref transform);

            DrawPlots();


            // --- Render Text ---
            GL.UseProgram(_textShaderProgram);
            // Use an orthographic projection matching the control's dimensions for the text
            var textTransform = Matrix4.CreateOrthographicOffCenter(0, _glControl.ClientSize.Width, 0, _glControl.ClientSize.Height, -1.0f, 1.0f);
            int textTransformLocation = GL.GetUniformLocation(_textShaderProgram, "uTransform");
            GL.UniformMatrix4(textTransformLocation, false, ref textTransform);

            // Also, you need to tell the shader which texture unit to use
            int textureLocation = GL.GetUniformLocation(_textShaderProgram, "uTexture");
            GL.Uniform1(textureLocation, 0); // Use texture unit 0

            // And set the text color
            int colorLocation = GL.GetUniformLocation(_textShaderProgram, "uColor");
            GL.Uniform4(colorLocation, Color.Black);

            int smoothingLocation = GL.GetUniformLocation(_textShaderProgram, "uSmoothing");
            GL.Uniform1(smoothingLocation, 0.05f);

            int thresholdLocation = GL.GetUniformLocation(_textShaderProgram, "uThreshold");
            GL.Uniform1(thresholdLocation, 0.75f); // <-- EXPERIMENT WITH THIS VALUE!

            DrawText();

            _glControl.SwapBuffers();
        }

        // --- Methods for Subclasses ---
        protected abstract void Init();
        protected abstract void DrawPlots();
        protected abstract void DrawText();
        protected abstract void ShutDown();

        // --- Resource Management ---

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderTimer.Dispose();

                if (_isLoaded)
                {
                    _isLoaded = false;

                    Thread.Sleep(100); // Allow time for the render loop to finish

                    ShutDown();

                    ShaderManager.Clear();
                }
            }
            base.Dispose(disposing);
        }
    }
}
