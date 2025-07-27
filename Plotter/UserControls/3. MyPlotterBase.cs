using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.ComponentModel;
using TestPlot;

namespace Plotter.UserControls
{
    [ToolboxItem(false)]
    internal abstract class MyPlotterBase : MyGLControl
    {

        // Shader programs
        protected int _plotShaderProgram ;


        protected MyPlotterBase()
        {
            if (!Program.IsRunning) return;
        }

        protected override void Init()
        {
            _plotShaderProgram = ShaderManager.Get("plot");
        }

        protected override void Render()
        {

            GL.UseProgram(_plotShaderProgram);

            // --- Calculate and Upload Transformation Matrix ---
            var transform = Matrix4.CreateOrthographicOffCenter(ViewPort.Left, ViewPort.Right, ViewPort.Top, ViewPort.Bottom, -1.0f, 1.0f);
            int transformLocation = GL.GetUniformLocation(_plotShaderProgram, "uTransform");
            GL.UniformMatrix4(transformLocation, false, ref transform);

            DrawPlots();
        }

        protected abstract void DrawPlots();

        
    }
}
