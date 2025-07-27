using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Plotter.Fonts;
using System.ComponentModel;
using TestPlot;
using Timer = System.Windows.Forms.Timer;

namespace Plotter.UserControls
{
    [ToolboxItem(false)]
    internal class MyGLControl : UserControl
    {
        private readonly GLControl _glControl = default!;
        private readonly Timer _renderTimer = default!;
        protected int _textShaderProgram;

        public bool IsLoaded => _isLoaded;
        private bool _isLoaded = false;

        protected FontFile? font;
        protected FontRenderer? fontRenderer;

        // --- Methods for Subclasses ---
        protected virtual void Init() { }
        protected virtual void Render() { }
        protected virtual void DrawText() { }
        protected virtual void ShutDown() { }


        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RectangleF ViewPort { get; set; } = new(0, 1, 100, 2);


        public MyGLControl()
        {
            var glControlSettings = new GLControlSettings
            {
                NumberOfSamples = 4,
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core,
                API = ContextAPI.OpenGL
            };
            _glControl = new(glControlSettings) { Dock = DockStyle.Fill };
            this.Controls.Add(_glControl);

            this.Load += GL_Load;
            this.Resize += GL_Resize;

            _renderTimer = new Timer() { Interval = 15 };
            _renderTimer.Tick += RenderLoop;
        }

        /// <summary>
        /// Final setup method that initializes OpenGL, shaders, and plots.
        /// </summary>
        private void GL_Load(object? sender, EventArgs e)
        {
            if (IsLoaded || IsDisposed) return;

            GL.Viewport(0, 0, _glControl.ClientSize.Width, _glControl.ClientSize.Height);

            GL.ClearColor(Color.Gainsboro);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            font = FontLoader.Load("Roboto-Medium.json");
            fontRenderer = new();

            _textShaderProgram = ShaderManager.Get("msdf");
            Init();

            _isLoaded = true;
            _renderTimer.Start();
        }
        
        private void GL_Resize(object? sender, EventArgs e)
        {
            if (!_isLoaded) return;

            GL.Viewport(0, 0, _glControl.ClientSize.Width, _glControl.ClientSize.Height);
        }

        /// <summary>
        /// The main render loop. Renders all registered plots.
        /// </summary>
        private void RenderLoop(object? sender, EventArgs e)
        {
            if (!IsLoaded || IsDisposed) return;

            _glControl.MakeCurrent();
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Render();

            RenderText();

            _glControl.SwapBuffers();
        }

        private void RenderText()
        {
            GL.UseProgram(_textShaderProgram);

            // (0,0) is bottom-left corner, opposite of Windows Forms
            var textTransform = Matrix4.CreateOrthographicOffCenter(0, _glControl.ClientSize.Width, 0, _glControl.ClientSize.Height, -1.0f, 1.0f);
            int textTransformLocation = GL.GetUniformLocation(_textShaderProgram, "uTransform");
            GL.UniformMatrix4(textTransformLocation, false, ref textTransform);

            int textureLocation = GL.GetUniformLocation(_textShaderProgram, "uTexture");
            GL.Uniform1(textureLocation, 0);

            int colorLocation = GL.GetUniformLocation(_textShaderProgram, "uColor");
            GL.Uniform4(colorLocation, Color.Black);

            DrawText();
        }

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
