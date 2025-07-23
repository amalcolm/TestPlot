using OpenTK.Graphics.OpenGL4;

namespace Plotter
{

    /// <summary>
    /// Represents a parsed .fnt file, containing all character and metric data.
    /// </summary>
    public class FontFile : IDisposable
    {
        public static FontFile Default { get; set; } = default!;

        public string Face          { get; private set; } = string.Empty;
        public int    Size          { get; private set; }
        public int    LineHeight    { get; private set; }
        public int    Base          { get; private set; }
        public int    TextureWidth  { get; private set; }
        public int    TextureHeight { get; private set; }
        public string TextureFile   { get; private set; } = string.Empty;

        public Dictionary<int, FontChar> Chars { get; } = [];
        public Dictionary<(int, int), int> Kernings { get; } = [];

        internal FontFile() { }

        internal void SetInfo(string face, int size)
        {
            Face = face;
            Size = size;

            if (face == "Segoe UI") Default = this; // Set default font if it's Segoe UI
        }

        internal void SetCommon(int lineHeight, int baseHeight, int textureWidth, int textureHeight)
        {
            LineHeight    = lineHeight;
            Base          = baseHeight;
            TextureWidth  = textureWidth;
            TextureHeight = textureHeight;
        }

        internal void SetPage(string textureFile)
            => TextureFile = textureFile;

        /// <summary>
        /// Gets the kerning adjustment between two characters.
        /// </summary>
        public int GetKerning(char first, char second) 
            => Kernings.TryGetValue(((int)first, (int)second), out var amount) ? amount : 0;

        private int _textureId = -1;


        // Called by FontLoader after the texture is created
        internal void SetTextureId(int textureId)
        {
            _textureId = textureId;
        }

        /// <summary>
        /// Activates and binds the font's texture to the specified texture unit.
        /// </summary>
        /// <param name="unit">The texture unit to bind to (e.g., TextureUnit.Texture0).</param>
        public void UseTexture(TextureUnit unit = TextureUnit.Texture0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
        }

        public void Dispose()
        {
            // Ensure the OpenGL resource is released when the object is disposed
            if (_textureId != -1) GL.DeleteTexture(_textureId);
            
            GC.SuppressFinalize(this);
        }
    }
}
