using OpenTK.Graphics.OpenGL4;

namespace Plotter.Fonts
{
    struct TextBlock(string text, float x, float y, FontFile? font)
    {
        public string    Text  = text;
        public FontFile  Font  = font ?? FontFile.Default;
        public float     X     = x;
        public float     Y     = y;
    }

    internal class FontRenderer : IDisposable
    {
        private readonly int _vbo;
        private readonly int _vao;
        private int _vertexCount = 0;
        private int _bufferSize = 0; // Current size of the VBO in vertices

        public string Text { get; set; } = string.Empty;
        public FontFile Font { get; set; } = default!;


        private List<FontVertex> _vertices = [];
        public FontRenderer()
        {
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

        public void RenderText(string text, float x, float y, FontFile? font = null)
        {   if (string.IsNullOrEmpty(text)) return;

            if (text != Text)
            { 
                Text = text;
                _vertices = FontVertex.BuildString(text, font ?? FontFile.Default, x, y);
                BindVertices();
            }
            Render();
        }

        public void RenderText(TextBlock block)
            => RenderText(block.Text, block.X, block.Y, block.Font);

        public void RenderText(IEnumerable<TextBlock> blocks)
        {
            var text = string.Join("\n", blocks.Select(b => b.Text));
            if (text != Text)
            {
                Text = text;
                _vertices.Clear();

                foreach (var block in blocks)
                    _vertices.AddRange(FontVertex.BuildString(block.Text, block.Font, block.X, block.Y));

                BindVertices();
            }
            Render();
        }

        public void BindVertices()
        {
            if (_vertices.Count == 0) return;

            _vertexCount = _vertices.Count;

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

            int requiredSize = _vertexCount * sizeof(float) * 4;
            _bufferSize = Math.Max(_bufferSize, requiredSize);

            GL.BufferData(BufferTarget.ArrayBuffer, _bufferSize, IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, requiredSize, _vertices.ToArray());
        }

        

        public void Render()
        {
            if (_vertexCount == 0) return;

            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
            GC.SuppressFinalize(this);
        }
    }
}
