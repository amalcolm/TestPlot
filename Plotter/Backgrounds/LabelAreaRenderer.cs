using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Plotter.UserControls;
using StbImageSharp;

namespace Plotter.Backgrounds
{
    internal class LabelAreaRenderer
    {
        private int _bgVao, _bgVbo, _bgTextureId, _bgShaderProgram;

        public LabelAreaRenderer(MyGLControl myGL, string texturePath)
        {
            myGL.Enqueue(() => InitBackground(texturePath));
        }

        private void InitBackground(string texturePath)
        {
            _bgShaderProgram = ShaderManager.Get("back");
            _bgVao = GL.GenVertexArray();
            _bgVbo = GL.GenBuffer();

            GL.BindVertexArray(_bgVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _bgVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 16, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, sizeof(float) * 2);

            GL.BindVertexArray(0);
            _bgTextureId = LoadTexture(texturePath);
        }

        public void Render(RectangleF bounds, Matrix4 projection)
        {
            if (bounds.IsEmpty) return;

            // Activate the background shader
            GL.UseProgram(_bgShaderProgram);

            GL.BindVertexArray(_bgVao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _bgTextureId);

            GL.UniformMatrix4(GL.GetUniformLocation(_bgShaderProgram, "uTransform"), false, ref projection);
            GL.Uniform1(GL.GetUniformLocation(_bgShaderProgram, "uTexture"), 0);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            UpdateBackgroundVertices(bounds);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            // Clean up state
            GL.Disable(EnableCap.Blend);
            GL.BindVertexArray(0);
        }

        // UpdateBackgroundVertices and LoadTexture methods remain the same as in your file.
        private void UpdateBackgroundVertices(RectangleF bounds)
        {
            float[] vertices =
            [
                bounds.Left,  bounds.Bottom,  0.0f, 1.0f,
            bounds.Left,  bounds.Top,     0.0f, 0.0f,
            bounds.Right, bounds.Bottom,  1.0f, 1.0f,
            bounds.Right, bounds.Top,     1.0f, 0.0f,
        ];

            GL.BindBuffer(BufferTarget.ArrayBuffer, _bgVbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices.Length * sizeof(float), vertices);
        }

        private static int LoadTexture(string filePath)
        {
            if (!File.Exists(filePath))
            {
                int handle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, handle);
                byte[] data = [255, 255, 255, 128];
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 1, 1, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                return handle;
            }

            int textureHandle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureHandle);
            using (Stream stream = File.OpenRead(filePath))
            {
                ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
            }
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
            return textureHandle;
        }

        public void Shutdown()
        {
            GL.DeleteBuffer(_bgVbo);
            GL.DeleteVertexArray(_bgVao);
            GL.DeleteTexture(_bgTextureId);
        }
    }
}