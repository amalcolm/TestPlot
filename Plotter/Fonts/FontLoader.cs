using OpenTK.Graphics.OpenGL4;

namespace Plotter
{
    using SkiaSharp;
    using System.Text.RegularExpressions;

    public static class FontLoader
    {
        public static FontFile Load(string filePath)
        {
            var fontFile = new FontFile();
            if (!filePath.Contains('\\')) filePath = $@"Resources\Fonts\{filePath}";
            var lines = File.ReadLines(filePath);

            foreach (var line in lines)
            {
                var parts = SplitLine(line);
                if (parts.Length < 1) continue;

                var values = ParseValues(parts);

                switch (parts[0])
                {
                    case "info":
                        fontFile.SetInfo(values["face"].Trim('"'), int.Parse(values["size"]));
                        break;

                    case "common":
                        fontFile.SetCommon(
                            int.Parse(values["lineHeight"]),
                            int.Parse(values["base"      ]),
                            int.Parse(values["scaleW"    ]),
                            int.Parse(values["scaleH"    ])
                        );
                        break;

                    case "page":
                        fontFile.SetPage(values["file"].Trim('"'));
                        string? directory = Path.GetDirectoryName(filePath);
                        string filename = Path.GetFileNameWithoutExtension(filePath);
                        string texturePath = Path.Combine(directory ?? "", $"{filename}.png");

                        int textureId = LoadTexture(texturePath);
                        fontFile.SetTextureId(textureId);
                        break;

                    case "char":
                        var fontChar = new FontChar
                        {
                            ID       = int.Parse(values["id"      ]),
                            X        = int.Parse(values["x"       ]),
                            Y        = int.Parse(values["y"       ]),
                            Width    = int.Parse(values["width"   ]),
                            Height   = int.Parse(values["height"  ]),
                            XOffset  = int.Parse(values["xoffset" ]),
                            YOffset  = int.Parse(values["yoffset" ]),
                            XAdvance = int.Parse(values["xadvance"])
                        };
                        fontFile.Chars[fontChar.ID] = fontChar;
                        break;

                    case "kerning":
                        fontFile.Kernings[(int.Parse(values["first"]), int.Parse(values["second"]))] = int.Parse(values["amount"]);
                        break;
                }
            }
            return fontFile;
        }

        private static string[] SplitLine(string line)
        {
            // Use regex to split by spaces, but preserve quoted strings
            var regex = new Regex("[^\\s\"']+|\"([^\"]*)\"|'([^']*)'");
            return [.. regex.Matches(line).Cast<Match>().Select(m => m.Value)];
        }

        private static Dictionary<string, string> ParseValues(string[] parts)
        {
            var values = new Dictionary<string, string>();
            for (int i = 1; i < parts.Length; i++)
            {
                var pair = parts[i].Split(new[] { '=' }, 2);
                if (pair.Length == 2)
                {
                    values[pair[0]] = pair[1];
                }
            }
            return values;
        }


        private static int LoadTexture(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Font texture not found.", filePath);

            int handle = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, handle);

            using var image = SKBitmap.Decode(filePath);

            // Flip the image vertically to match OpenGL's coordinate system
            using var flipped = new SKBitmap(image.Width, image.Height);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, flipped.Width, flipped.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, image.Pixels);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            return handle;
        }
    }

}
