using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Plotter.Fonts;
using System.Collections.Concurrent;
using System.ComponentModel;
using TestPlot;
using Timer = System.Windows.Forms.Timer;

namespace Plotter.UserControls
{
    [ToolboxItem(false)]
    internal class MyGLControl : UserControl
    {
        static int InstanceCount = 0;

        public MyGLThread GLThread { get; private set; }
        public void Enqueue(Action? initAction, Action? shutdownAction = null) 
            => GLThread.Enqueue(initAction, shutdownAction);
        

        static ConcurrentDictionary<string, FontFile> _fontCache = [];
        public static FontFile GetFont(string name)
        {
            if (_fontCache.TryGetValue(name, out var font))
                return font;
            font = FontLoader.Load(name);
            _fontCache[name] = font;
            return font;
        }

        protected readonly GLControl MyGL = default!;
        protected int _textShaderProgram;

        public bool IsLoaded => _isLoaded;
        private bool _isLoaded = false;

        protected FontFile? font;
        protected FontRenderer fontRenderer = new();

        // --- Methods for Subclasses ---
        protected virtual void Init() { }
        protected virtual void Render() { }
        protected virtual void DrawText() { }
        protected virtual void Shutdown() { }


        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RectangleF ViewPort { get; set; } = new(0, 1, 100, 2);


        public MyGLControl()
        {
            InstanceCount++;

            var glControlSettings = new GLControlSettings
            {
                NumberOfSamples = 4,
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core,
                API = ContextAPI.OpenGL
            };
            MyGL = new(glControlSettings) { Dock = DockStyle.Fill };
            this.Controls.Add(MyGL);

            GLThread = new(MyGL);

            this.Load += (s,e) => GLThread.Enqueue(GL_Load, GL_Shutdown);
            this.Resize += (s,e) => GLThread.Enqueue(GL_Resize);
            GLThread.RenderAction = RenderLoop;
        }

        /// <summary>
        /// Final setup method that initializes OpenGL, shaders, and plots.
        /// </summary>
        private void GL_Load()
        {
            if (IsLoaded || IsDisposed) return;

            GL.Viewport(0, 0, MyGL.ClientSize.Width, MyGL.ClientSize.Height);

            GL.ClearColor(Color.Gainsboro);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _textShaderProgram = ShaderManager.Get("msdf");
            font = GetFont("Roboto-Medium.json");
            fontRenderer.Init();
            if (ParentForm != null)
                ParentForm.FormClosing += (s, e) => _isLoaded = false;


            Init();
            _isLoaded = true;
        }
        
        private void GL_Resize()
        {   if (!_isLoaded) return;

            GL.Viewport(0, 0, MyGL.ClientSize.Width, MyGL.ClientSize.Height);
        }

        /// <summary>
        /// The main render loop. Renders all registered plots.
        /// </summary>
        private void RenderLoop()
        {
            if (!IsLoaded || IsDisposed) return;

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Render();

            RenderText();

            MyGL.SwapBuffers();
        }

        private void RenderText()
        {
            GL.UseProgram(_textShaderProgram);

            // (0,0) is bottom-left corner, opposite of Windows Forms
            var textTransform = Matrix4.CreateOrthographicOffCenter(0, MyGL.ClientSize.Width, 0, MyGL.ClientSize.Height, -1.0f, 1.0f);
            int textTransformLocation = GL.GetUniformLocation(_textShaderProgram, "uTransform");
            GL.UniformMatrix4(textTransformLocation, false, ref textTransform);

            int textureLocation = GL.GetUniformLocation(_textShaderProgram, "uTexture");
            GL.Uniform1(textureLocation, 0);

            int colorLocation = GL.GetUniformLocation(_textShaderProgram, "uColor");
            GL.Uniform4(colorLocation, Color.Black);

            DrawText();
        }

        protected void GL_Shutdown()
        {
            if (_isLoaded)
            {
                _isLoaded = false;

                Shutdown();

                InstanceCount--;

                if (InstanceCount == 0)
                {
                    fontRenderer.Shutdown();

                    foreach (var font in _fontCache.Values)
                        GL.DeleteTexture(font.TextureId);

                    ShaderManager.Clear();
                }
            }
        }


    }
}
