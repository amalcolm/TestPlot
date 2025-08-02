using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Plotter.Fonts;
using Plotter.UserControls;
using StbImageSharp;

namespace Plotter.Backgrounds
{
    internal class LabelAreaRenderer
    {
        private readonly MyGLControl _myGL;
        private readonly FontRenderer _fontRenderer;

        // For the background
        private int _bgVao;
        private int _bgVbo;
        private int _bgTextureId;
        private int _bgShaderProgram;
        private RectangleF _lastBounds = RectangleF.Empty;

        public LabelAreaRenderer(MyGLControl myGL, string texturePath)
        {
            _myGL = myGL;
            _fontRenderer = new FontRenderer();
            myGL.Enqueue(() =>
            {
                _fontRenderer.Init();
                InitBackground(texturePath);
            });
        }

        private void InitBackground(string texturePath)
        {
            // You'll need a simple shader for drawing a textured quad.
            _bgShaderProgram = ShaderManager.Get("back");

            _bgVao = GL.GenVertexArray();
            _bgVbo = GL.GenBuffer();

            GL.BindVertexArray(_bgVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _bgVbo);
            // Allocate space for 4 _vertices, with 2 for position and 2 for tex coords
            GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * 16, IntPtr.Zero, BufferUsageHint.DynamicDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, sizeof(float) * 2);

            GL.BindVertexArray(0);

            _bgTextureId = LoadTexture(texturePath);
        }

        public void Render(IEnumerable<TextBlock> textBlocks, FontFile font)
        {
            if (!textBlocks.Any()) return;

            var bounds = CalculateBounds(textBlocks, font);
            if (bounds.IsEmpty) return;

            float padding = 10f;
            var paddedBounds = new RectangleF(
                bounds.X - padding,
                bounds.Y - padding,
                bounds.Width + padding * 2,
                bounds.Height + padding * 2
            );

            var projection = Matrix4.CreateOrthographicOffCenter(0, _myGL.ClientSize.Width, 0, _myGL.ClientSize.Height, -1.0f, 1.0f);

            RenderBackground(paddedBounds, projection);
            _fontRenderer.Font = font;
            _fontRenderer.RenderText(textBlocks);
        }

        private void RenderBackground(RectangleF bounds, Matrix4 projection)
        {
            if (bounds != _lastBounds)
            {
                UpdateBackgroundVertices(bounds);
                _lastBounds = bounds;
            }

            GL.UseProgram(_bgShaderProgram);
            GL.BindVertexArray(_bgVao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _bgTextureId);

            GL.UniformMatrix4(GL.GetUniformLocation(_bgShaderProgram, "uTransform"), false, ref projection);
            GL.Uniform1(GL.GetUniformLocation(_bgShaderProgram, "uTexture"), 0);

            // Enable blending for transparency
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

            GL.BindVertexArray(0);
            GL.Disable(EnableCap.Blend);
        }

        private void UpdateBackgroundVertices(RectangleF bounds)
        {
            float[] vertices =
            [
                bounds.Left,  bounds.Bottom,  0.0f, 1.0f, // Bottom-left
                bounds.Left,  bounds.Top,     0.0f, 0.0f, // Top-left
                bounds.Right, bounds.Bottom,  1.0f, 1.0f, // Bottom-right
                bounds.Right, bounds.Top,     1.0f, 0.0f, // Top-right
            ];

            GL.BindBuffer(BufferTarget.ArrayBuffer, _bgVbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertices.Length * sizeof(float), vertices);
        }

        private static RectangleF CalculateBounds(IEnumerable<TextBlock> textBlocks, FontFile font)
        {
            if (!textBlocks.Any()) return RectangleF.Empty;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var block in textBlocks)
            {
                var blockBounds = block.Bounds;
                if (!blockBounds.IsEmpty)
                {
                    minX = Math.Min(minX, blockBounds.Left);
                    minY = Math.Min(minY, blockBounds.Top);
                    maxX = Math.Max(maxX, blockBounds.Right);
                    maxY = Math.Max(maxY, blockBounds.Bottom);
                }
            }

            if (minX == float.MaxValue) return RectangleF.Empty;

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        private static int LoadTexture(string filePath)
        {
            // Create a placeholder texture if the file doesn't exist.
            if (!File.Exists(filePath))
            {
                int handle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, handle);
                byte[] data = [255, 255, 255, 128]; // white, 50% transparent
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
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            return textureHandle;
        }

        public void Shutdown()
        {
            _fontRenderer?.Shutdown();
            GL.DeleteBuffer(_bgVbo);
            GL.DeleteVertexArray(_bgVao);
            GL.DeleteTexture(_bgTextureId);
        }
    }
}