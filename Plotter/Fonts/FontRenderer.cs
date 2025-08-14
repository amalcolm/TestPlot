using OpenTK.Graphics.OpenGL4;

namespace Plotter.Fonts
{
    
    internal class FontRenderer
    {
        public float Scaling { get; set; } = 0.5f;

        private int _vbo;
        private int _vao;
        private int _bufferSize = 0; // Current size of the VBO in _vertices

        public FontFile Font { get; set; } = default!;


        private FontVertex[] _vertices = [];
        private int _currentVertexCount = 0;
        public void Init()
        {
            _vertices = new FontVertex[1024]; // Initial capacity for ~170 characters

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

            // Configure the vertex attributes for FontVertex
            GL.EnableVertexAttribArray(0); // Position
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, 0);

            GL.EnableVertexAttribArray(1); // TexCoord
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * 4, sizeof(float) * 2);

            GL.BindVertexArray(0);
        }
        public void RenderText(ReadOnlySpan<char> text, float x, float y, FontFile? font = null, TextAlign textAlign = TextAlign.Left)
        {
            if (text.IsEmpty)
            {
                _currentVertexCount = 0; // Ensure nothing is drawn if text is empty
                return;
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, font?.TextureId ?? FontFile.Default.TextureId);

            // Ensure our vertex array is big enough and then build the string into it
            EnsureVertexCapacity(text.Length * 6);
            _currentVertexCount = FontVertex.BuildString(_vertices, 0, text, font ?? FontFile.Default, x, y, Scaling, textAlign);

            BindVertices();
            Render();
        }


        public void RenderText(string text, float x, float y, FontFile? font = null, TextAlign textAlign = TextAlign.Left)
            => RenderText(text.AsSpan(), x, y, font, textAlign);

        public void RenderText(TextBlock block)
            => RenderText(block.Span, block.X, block.Y, block.Font, block.Align);

        public void RenderText(IEnumerable<TextBlock> blocks)
        {
            _currentVertexCount = 0;

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, blocks.FirstOrDefault()?.Font.TextureId ?? FontFile.Default.TextureId);

            foreach (var block in blocks)
            {
                var blockVerticesSpan = block.GetVertices(Scaling);
                EnsureVertexCapacity(_currentVertexCount + blockVerticesSpan.Length);
                blockVerticesSpan.CopyTo(_vertices.AsSpan(_currentVertexCount));
                _currentVertexCount += blockVerticesSpan.Length;
            }

            BindVertices();
            Render();
        }

        public void BindVertices()
        {
            if (_currentVertexCount == 0) return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

            int requiredSize = _currentVertexCount * sizeof(float) * 4;
            if (requiredSize > _bufferSize)
            {
                _bufferSize = requiredSize;
                GL.BufferData(BufferTarget.ArrayBuffer, _bufferSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
            }

            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, requiredSize, _vertices);
        }

        public void Render()
        {
            if (_currentVertexCount == 0) return;

            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _currentVertexCount);
            GL.BindVertexArray(0);
        }

        public void Shutdown()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
            GC.SuppressFinalize(this);
        }

        private void EnsureVertexCapacity(int requiredCount)
        {
            if (_vertices.Length < requiredCount)
            {
                int newSize = Math.Max(_vertices.Length * 2, requiredCount);
                Array.Resize(ref _vertices, newSize);
            }
        }
    }
}
