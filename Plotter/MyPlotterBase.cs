﻿using OpenTK.Mathematics;
using OpenTK.GLControl;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using System.ComponentModel;
using TestPlot;
using Timer = System.Windows.Forms.Timer;

namespace Plotter
{
    [ToolboxItem(false)]
    internal abstract class MyPlotterBase : UserControl
    {
        private readonly GLControl _glControl = default!;
        private readonly Timer _renderTimer = default!;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RectangleF ViewPort { get; set; } = new(0, 1, 100, 2);

        // Shader programs
        protected int _plotShaderProgram ;
        protected int _textShaderProgram;

        public bool IsLoaded => _isLoaded;
        private bool _isLoaded = false;

        protected MyPlotterBase()
        {
            if (!Program.IsRunning) return;

            // Basic control setup
            var glControlSettings = new GLControlSettings
            {
                NumberOfSamples = 4,
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core,
                API = ContextAPI.OpenGL
            };
            _glControl = new(glControlSettings) { Dock = DockStyle.Fill };
            this.Controls.Add(_glControl);

            // Hook the one-time load event
            _glControl.Load += OnLoad;
            _glControl.Resize += OnResize;

            _renderTimer = new Timer() { Interval = 15 };
            _renderTimer.Tick += Render;
        }

        /// <summary>
        /// Final setup method that initializes OpenGL, shaders, and plots.
        /// </summary>
        private void OnLoad(object? sender, EventArgs e)
        {
            if (IsLoaded || IsDisposed) return;

            GL.Viewport(0, 0, _glControl.ClientSize.Width, _glControl.ClientSize.Height);

            GL.ClearColor(Color.Gainsboro);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _plotShaderProgram = ShaderManager.Get("plot");
            _textShaderProgram = ShaderManager.Get("msdf");
            Init();

            _isLoaded = true;
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
            var transform = Matrix4.CreateOrthographicOffCenter(ViewPort.Left, ViewPort.Right, ViewPort.Top, ViewPort.Bottom, -1.0f, 1.0f);
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
