using OpenTK.Mathematics;

namespace Plotter.Fonts
{
    internal struct FontVertex
    {
        public Vector2 Position; // The (X, Y) position on the screen
        public Vector2 TexCoord; // The (U, V) coordinate on the font atlas texture

        public static List<FontVertex> BuildString(List<FontVertex> vertices, AString text, FontFile font, float startX, float startY, float scaling = 1.0f, TextAlign textAlign = TextAlign.Right)
        {
            vertices.Clear();
            if (text.Length == 0)
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
                        cursor.X += font.GetKerning(text[i - 1], text[i]) * scaling;

                    BuildChar(vertices, fc, cursor, scaling, font, texWidth, texHeight);
                    cursor.X += fc.XAdvance * scaling;
                }
            }
            else 
            {
                for (int i = text.Length - 1; i >= 0; i--)
                {
                    if (!font.Chars.TryGetValue(text[i], out FontChar fc)) continue;

                    cursor.X -= fc.XAdvance * scaling;

                    if (i > 0)
                        cursor.X += font.GetKerning(text[i - 1], text[i]) * scaling;

                    BuildChar(vertices, fc, cursor, scaling, font, texWidth, texHeight);
                }
            }
            return vertices;
        }

        private static void BuildChar(List<FontVertex> vertices, FontChar fontChar, Vector2 cursor, float scaling, FontFile font, float texWidth, float texHeight)
        { 
            float x_pos  = cursor.X + scaling *  fontChar.XOffset;
            float y_pos  = cursor.Y - scaling * (fontChar.YOffset - font.Base + fontChar.Height); 
            float width  = fontChar.Width  * scaling;
            float height = fontChar.Height * scaling;

            float u1 =  fontChar.X                    / texWidth;
            float v1 =  fontChar.Y                    / texHeight;
            float u2 = (fontChar.X + fontChar.Width ) / texWidth;
            float v2 = (fontChar.Y + fontChar.Height) / texHeight;

            // Return six _vertices for two triangles (quad) for this character
            vertices.Add(new FontVertex { Position = new Vector2(x_pos        , y_pos + height),   /* Top-left      */  TexCoord = new Vector2(u1, v1) });
            vertices.Add(new FontVertex { Position = new Vector2(x_pos + width, y_pos         ),   /* Bottom-right  */  TexCoord = new Vector2(u2, v2) });
            vertices.Add(new FontVertex { Position = new Vector2(x_pos        , y_pos         ),   /* Bottom-left   */  TexCoord = new Vector2(u1, v2) });
            vertices.Add(new FontVertex { Position = new Vector2(x_pos        , y_pos + height),   /* Top-left      */  TexCoord = new Vector2(u1, v1) });
            vertices.Add(new FontVertex { Position = new Vector2(x_pos + width, y_pos + height),   /* Top-right     */  TexCoord = new Vector2(u2, v1) });
            vertices.Add(new FontVertex { Position = new Vector2(x_pos + width, y_pos         ),   /* Bottom-right  */  TexCoord = new Vector2(u2, v2) });
        }
    }
}
