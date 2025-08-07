
namespace Plotter.Fonts
{
    public enum TextAlign { Left, Right }

    class TextBlock(string text, float x, float y, FontFile? font, TextAlign textAlign = TextAlign.Left) : IDisposable
    {
        public string    oldText  {get => new(Span);  set { SetValue(text.AsSpan()); /* Changed is called inside SetValue */ } }

        public FontFile  Font  {get => _font;      set { if (_font  != value) { _font  = value; Changed(nameof(Font )); } } }
        public float     X     {get => _x;         set { if (_x     != value) { _x     = value; Changed(nameof(X    )); } } }
        public float     Y     {get => _y;         set { if (_y     != value) { _y     = value; Changed(nameof(Y    )); } } }
        public TextAlign Align {get => _align;     set { if (_align != value) { _align = value; Changed(nameof(Align)); } } }


        public ReadOnlySpan<char> Span => _buffer.AsSpan(0, _length);

        public void SetValue(ReadOnlySpan<char> text)
        {
            if (Span.SequenceEqual(text)) return;

            ClearBuffer(); // Return old buffer if it exists
            _buffer = CharPool.Rent();
            text.CopyTo(_buffer);
            _length = text.Length;
            Changed("Text");
        }

        public void SetValue(double value, string format = "F2")
        {
            char[] tempBuffer = CharPool.Rent();
            if (value.TryFormat(tempBuffer, out int charsWritten, format))
            {
                var newSpan = tempBuffer.AsSpan(0, charsWritten);

                if (!Span.SequenceEqual(newSpan))
                {
                    ClearBuffer(); // Return the old buffer
                    _buffer = tempBuffer; // Keep the new buffer
                    _length = charsWritten;
                    Changed(nameof(oldText));
                    return; // Success
                }
            }

            CharPool.Return(tempBuffer);
        }

        // Clean up the buffer
        private void ClearBuffer()
        {
            if (_buffer != null)
            {
                CharPool.Return(_buffer);
                _buffer = null;
                _length = 0;
            }
        }

        public void Dispose()
        {
            ClearBuffer();
            GC.SuppressFinalize(this);
        }


        public RectangleF Bounds = RectangleF.Empty;

        private char[]?   _buffer;
        private int       _length = 0;
        private FontFile  _font   = font ?? FontFile.Default;
        private float     _x      = x;
        private float     _y      = y;
        private TextAlign _align  = textAlign;

        public int hashCode { get; private set; } = 0;
        private void Changed(string _)
        {
            _hasChanged = true;
            hashCode = 0;
        }
        override public int GetHashCode()
        {
            if (hashCode == 0 && _length > 0)
                hashCode = string.GetHashCode(Span);

            return hashCode;
        }


        private bool _hasChanged = false;

        override public string ToString() => $"{oldText} ({X}, {Y}) [{Font.Face} {Font.Size}] {Align}";
        override public bool Equals(object? obj)
        {
            if (obj is TextBlock other)
                return Span.SequenceEqual(other.Span);
            return false;
        }

        private List<FontVertex> _vertices = [];

        public List<FontVertex> GetVertices(float scaling = 0.5f)
        {
            if (_hasChanged || _vertices.Count == 0)
            {
                FontVertex.BuildString(_vertices, Span, Font, X, Y, scaling, Align);
                Bounds = CalculateBoundsFromVertices();
                _hasChanged = false;
            }
            return _vertices;
        }


        private RectangleF CalculateBoundsFromVertices()
        {
            if (_vertices.Count == 0)
                return RectangleF.Empty;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            for (int i = 0; i < _vertices.Count; i += 6)
            {
                var topLeft = _vertices[i].Position;
                var bottomLeft = _vertices[i + 2].Position;
                var bottomRight = _vertices[i + 5].Position;

                minX = Math.Min(minX, bottomLeft.X);
                minY = Math.Min(minY, bottomLeft.Y);
                maxX = Math.Max(maxX, bottomRight.X);
                maxY = Math.Max(maxY, topLeft.Y);
            }

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
 
    }
}
