using OpenTK;

namespace Plotter
{
    internal struct FontVertex
    {
        public Vector2 Position; // The (X, Y) position on the screen
        public Vector2 TexCoord; // The (U, V) coordinate on the font atlas texture

        public static List<FontVertex> BuildString(string text, FontFile font, float startX, float startY)
        {
            var vertices = new List<FontVertex>();
            var cursor = new Vector2(startX, startY);

            // Get texture dimensions for normalizing coordinates
            float texWidth = font.TextureWidth;
            float texHeight = font.TextureHeight;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!font.Chars.TryGetValue(c, out FontChar fontChar))
                {   // Character not in font, skip or substitute
                    continue;
                }

                float x      = cursor.X + fontChar.XOffset;
                float y      = cursor.Y - fontChar.YOffset - fontChar.Height + font.Base;
                float width  = fontChar.Width ;
                float height = fontChar.Height;

                // These are normalized from 0.0 to 1.0
                float u1 =  fontChar.X           / texWidth;
                float v1 =  fontChar.Y           / texHeight;
                float u2 = (fontChar.X + width)  / texWidth;
                float v2 = (fontChar.Y + height) / texHeight;

                // Add six vertices for two triangles (quad) for this character
                vertices.Add(new FontVertex { Position = new Vector2(x        , y + height),   /* Top   -left  */  TexCoord = new Vector2(u1, v1) });
                vertices.Add(new FontVertex { Position = new Vector2(x + width, y         ),   /* Bottom-right */  TexCoord = new Vector2(u2, v2) });
                vertices.Add(new FontVertex { Position = new Vector2(x        , y         ),   /* Bottom-left  */  TexCoord = new Vector2(u1, v2) });
                vertices.Add(new FontVertex { Position = new Vector2(x        , y + height),   /* Top   -left  */  TexCoord = new Vector2(u1, v1) });
                vertices.Add(new FontVertex { Position = new Vector2(x + width, y + height),   /* Top   -right */  TexCoord = new Vector2(u2, v1) });
                vertices.Add(new FontVertex { Position = new Vector2(x + width, y         ),   /* Bottom-right */  TexCoord = new Vector2(u2, v2) });

                // Move cursor for the next character
                if (i + 1 >= text.Length) break; // Avoid out of bounds
                cursor.X += fontChar.XAdvance + font.GetKerning(c, text[i + 1]);
            }

            return vertices;
        }
    }
}
