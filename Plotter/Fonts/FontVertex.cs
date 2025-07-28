using OpenTK.Mathematics;

namespace Plotter.Fonts
{
    internal struct FontVertex
    {
        public Vector2 Position; // The (X, Y) position on the screen
        public Vector2 TexCoord; // The (U, V) coordinate on the font atlas texture

        public static List<FontVertex> BuildString(string text, FontFile font, float startX, float startY, TextAlign textAlign = TextAlign.Right)
        {
            var vertices = new List<FontVertex>();
            if (string.IsNullOrEmpty(text))
                return vertices;

            var cursor = new Vector2(startX, startY);
            float texWidth = font.TextureWidth;
            float texHeight = font.TextureHeight;

            if (textAlign == TextAlign.Left)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    if (!font.Chars.TryGetValue(text[i], out FontChar fc)) continue;

                    if (i > 0)
                        cursor.X += font.GetKerning(text[i - 1], text[i]);

                    BuildChar(vertices, fc, cursor, font, texWidth, texHeight);
                    cursor.X += fc.XAdvance;
                }
            }
            else 
            {
                for (int i = text.Length - 1; i >= 0; i--)
                {
                    if (!font.Chars.TryGetValue(text[i], out FontChar fc)) continue;

                    cursor.X -= fc.XAdvance;

                    if (i > 0)
                        cursor.X += font.GetKerning(text[i - 1], text[i]);

                    BuildChar(vertices, fc, cursor, font, texWidth, texHeight);
                }
            }
            return vertices;
        }

        private static void BuildChar(List<FontVertex> vertices, FontChar fontChar, Vector2 cursor, FontFile font, float texWidth, float texHeight)
        { 

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


        }
    }
}
