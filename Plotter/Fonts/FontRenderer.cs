using OpenTK.Graphics.OpenGL4;
using System.ComponentModel;

namespace Plotter.Fonts
{
    public enum TextAlign { Left, Right }

    class TextBlock(string text, float x, float y, FontFile? font, TextAlign textAlign = TextAlign.Left)
    {
        public string    Text  {get => _text;  set { if (_text  != value) { _text  = value; Changed(nameof(Text )); } } }
        public FontFile  Font  {get => _font;  set { if (_font  != value) { _font  = value; Changed(nameof(Font )); } } }
        public float     X     {get => _x;     set { if (_x     != value) { _x     = value; Changed(nameof(X    )); } } }
        public float     Y     {get => _y;     set { if (_y     != value) { _y     = value; Changed(nameof(Y    )); } } }
        public TextAlign Align {get => _align; set { if (_align != value) { _align = value; Changed(nameof(Align)); } } }

        private string    _text  = text;
        private FontFile  _font  = font ?? FontFile.Default;
        private float     _x     = x;
        private float     _y     = y;
        private TextAlign _align = textAlign;

        public int hashCode { get; private set; } = 0;
        private void Changed(string _)
        {
            _hasChanged = true;
            hashCode = (Text, $"{Font.Face}{Font.Size}", X, Y, Align).GetHashCode();
        }
        private bool _hasChanged = false;

        override public int GetHashCode() => hashCode;
        override public string ToString() => $"{Text} ({X}, {Y}) [{Font.Face} {Font.Size}] {Align}";
        override public bool Equals(object? obj)
        {
            if (obj is TextBlock other)
                return hashCode == other.hashCode;
            return false;
        }

        private List<FontVertex> vertices = [];

        public List<FontVertex> GetVertices(float scaling = 0.5f)
        {
            if (_hasChanged || vertices.Count == 0)
            {
                vertices = FontVertex.BuildString(Text, Font, X, Y, scaling, Align);
                _hasChanged = false;
            }
            return vertices;
        }
    }

    internal class FontRenderer
    {
        public float Scaling { get; set; } = 0.5f;

        private int _vbo;
        private int _vao;
        private int _vertexCount = 0;
        private int _bufferSize = 0; // Current size of the VBO in vertices

        public string Text { get; set; } = string.Empty;
        public FontFile Font { get; set; } = default!;


        private List<FontVertex> _vertices = [];
        public void Init()
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

        public void RenderText(string text, float x, float y, FontFile? font = null, TextAlign textAlign = TextAlign.Left)
        {   if (string.IsNullOrEmpty(text)) return;

            if (text != Text)
            { 
                Text = text;
                _vertices = FontVertex.BuildString(text, font ?? FontFile.Default, x, y, Scaling, textAlign);
                BindVertices();
            }
            Render();
        }

        public void RenderText(TextBlock block)
            => RenderText(block.Text, block.X, block.Y, block.Font, block.Align);

        public void RenderText(IEnumerable<TextBlock> blocks)
        {
            Text = "";
            _vertices.Clear();

            foreach (var block in blocks)
                _vertices.AddRange(block.GetVertices());

            BindVertices();
        
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

        public void Shutdown()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
            GC.SuppressFinalize(this);
        }
    }
}
