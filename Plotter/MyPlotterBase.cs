using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Timer = System.Windows.Forms.Timer;

namespace Plotter
{
    public abstract class MyPlotterBase : UserControl
    {
        private readonly GLControl _glControl;
        private readonly Timer _renderTimer;

        protected string LeadPlotKey { get; set; } = "";

        // Plot management
        private readonly Dictionary<string, MyGLPlot> _plots = [];

        // OpenGL state
        protected int _shaderProgram;
        public bool IsLoaded => _isLoaded;
        protected bool _isLoaded = false;

        protected MyPlotterBase()
        {
            // Basic control setup
            var mode = new OpenTK.Graphics.GraphicsMode(32, 24, 0, 4); // 32-bit color, 24-bit depth, 0 stencil, 4 samples for MSAA
            _glControl = new(mode) { Dock = DockStyle.Fill };
            this.Controls.Add(_glControl);

            // Hook the one-time load event
            _glControl.Load += OnLoad;
            this.Load += OnLoad;

            // Setup the active rendering loop
            _renderTimer = new Timer { Interval = 16 }; // ~60 FPS
            _renderTimer.Tick += (sender, args) => this.Render();
        }

        /// <summary>
        /// Final setup method that initializes OpenGL, shaders, and plots.
        /// This is sealed to ensure the base setup logic cannot be bypassed.
        /// </summary>
        private void OnLoad(object? sender, EventArgs e)
        {
            if (_isLoaded || IsDisposed) return;

            // --- 1. Compile and Link Shaders ---
            string vertexShaderSource = @"
                #version 330 core
                layout(location = 0) in vec3 aPosition;
                uniform mat4 uTransform;
            
                void main()
                {
                   gl_Position = uTransform * vec4(aPosition, 1.0);
                }";
            
            string fragmentShaderSource = "#version 330 core\nout vec4 FragColor;\nuniform vec4 uColor;\nvoid main()\n{\n   FragColor = uColor;\n}";

            _shaderProgram = CompileShaders(vertexShaderSource, fragmentShaderSource);

            // --- 2. Allow Derived Class to Create Plots ---
            // This abstract method MUST be implemented by the subclass.
            CreatePlots();

            // --- 3. Finalize Setup ---
            _isLoaded = true;
            _renderTimer.Start();
        }

        /// <summary>
        /// The main render loop. Renders all registered plots.
        /// </summary>

        private void Render()
        {
            if (!_isLoaded || IsDisposed) return;

            _glControl.MakeCurrent();
            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.UseProgram(_shaderProgram);

            // --- 2. Calculate and Upload Transformation Matrix ---
            var leadPlot = !string.IsNullOrEmpty(LeadPlotKey) ? GetPlot(LeadPlotKey) : null;
            Matrix4 transform = Matrix4.Identity;

            if (leadPlot != null && leadPlot.WindowSize > 0)
            {
                // Calculate the scaling factor to fit the window size into the -1 to +1 NDC space
                float scaleX = 2.0f / leadPlot.WindowSize;

                // Calculate the translation to move the latest point to the right edge
                float translateX = -((float)leadPlot.XCounter - leadPlot.WindowSize / 2.0f);

                // Create the transformation: first translate, then scale.
                transform = Matrix4.CreateTranslation(translateX, 0, 0) * Matrix4.CreateScale(scaleX, 1.0f, 1.0f);
            }

            int transformLocation = GL.GetUniformLocation(_shaderProgram, "uTransform");
            GL.UniformMatrix4(transformLocation, false, ref transform);

            // Iterate through all registered plots and render them
            foreach (var plot in _plots.Values)
            {
                // Allow the derived class to set plot-specific uniforms (e.g., color)
                OnRenderPlot(plot);

                // Tell the plot to draw itself
                plot.Render();
            }

            _glControl.SwapBuffers();
        }

        // --- Methods for Subclasses ---

        /// <summary>
        /// Hook for the derived class to create and add its plots.
        /// </summary>
        protected abstract void CreatePlots();

        /// <summary>
        /// Hook for the derived class to customize a plot before it is rendered.
        /// This is where you would set uniforms like color, line thickness, etc.
        /// </summary>
        /// <param name="plot">The plot that is about to be rendered.</param>
        protected abstract void OnRenderPlot(MyGLPlot plot);


        /// <summary>
        /// Adds a plot to the rendering engine. To be called from CreatePlots().
        /// </summary>
        protected void AddPlot(string key, MyGLPlot plot)
        {
            _plots.Add(key, plot);
        }

        /// <summary>
        /// Retrieves a plot by its key, allowing the derived class to add data to it.
        /// </summary>
        protected MyGLPlot? GetPlot(string key)
        {
            return _plots.TryGetValue(key, out var plot) ? plot : null;
        }

        // --- Resource Management ---

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderTimer.Stop();
                _renderTimer.Dispose();

                if (_isLoaded)
                {
                    foreach (var plot in _plots.Values)
                    {
                        plot.Dispose();
                    }
                    GL.DeleteProgram(_shaderProgram);
                }
            }
            base.Dispose(disposing);
        }

        // A helper method for compiling shaders (can be expanded with error checking)
        private static int CompileShaders(string vertexSource, string fragmentSource)
        {
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);

            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return program;
        }
    }
}
