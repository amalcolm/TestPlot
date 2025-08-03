
namespace Plotter.Fonts
{
    public enum TextAlign { Left, Right }

    class TextBlock(AString text, float x, float y, FontFile? font, TextAlign textAlign = TextAlign.Left)
    {
        public AString   Text  {get => _text;  set { if (_text  != value) { _text  = value; Changed(nameof(Text )); } } }
        public FontFile  Font  {get => _font;  set { if (_font  != value) { _font  = value; Changed(nameof(Font )); } } }
        public float     X     {get => _x;     set { if (_x     != value) { _x     = value; Changed(nameof(X    )); } } }
        public float     Y     {get => _y;     set { if (_y     != value) { _y     = value; Changed(nameof(Y    )); } } }
        public TextAlign Align {get => _align; set { if (_align != value) { _align = value; Changed(nameof(Align)); } } }


        public RectangleF Bounds = RectangleF.Empty;

        private AString  _text  = text;
        private FontFile  _font  = font ?? FontFile.Default;
        private float     _x     = x;
        private float     _y     = y;
        private TextAlign _align = textAlign;

        public int hashCode { get; private set; } = 0;
        private void Changed(string _)
        {
            _hasChanged = true;
            hashCode = Text.GetHashCode();
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

        private List<FontVertex> _vertices = [];

        public List<FontVertex> GetVertices(float scaling = 0.5f)
        {
            if (_hasChanged || _vertices.Count == 0)
            {
                FontVertex.BuildString(_vertices, Text, Font, X, Y, scaling, Align);
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
