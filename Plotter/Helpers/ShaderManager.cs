using OpenTK.Graphics.OpenGL4;

namespace Plotter
{
    internal static class ShaderManager
    {
        internal struct ShaderProgram
        {
            public string Name;

            public int ProgramId;

            public string FragmentShaderSource;
            public string VertexShaderSource;
        }

        private static Dictionary<string, ShaderProgram> _shaderPrograms = [];

        public static int Get(string name)
        {
            if (_shaderPrograms.Count == 0)
                Init();

            if (_shaderPrograms.TryGetValue(name, out var program))
                return program.ProgramId;

            throw new KeyNotFoundException($"Shader program '{name}' not found.");
        }

        private static void Init()
        {
            var files = Directory.GetFiles(@"Resources\Shaders", "*.*", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {   if (i+1 >= files.Length) break;

                var fragFile = files[i];
                var vertFile = files[i + 1];

                if (!fragFile.EndsWith(".frag", StringComparison.OrdinalIgnoreCase)) continue;
                if (!vertFile.EndsWith(".vert", StringComparison.OrdinalIgnoreCase)) continue;

                var program = new ShaderProgram
                {
                    Name = Path.GetFileNameWithoutExtension(fragFile),
                    FragmentShaderSource = File.ReadAllText(fragFile),
                    VertexShaderSource = File.ReadAllText(vertFile)
                };
                program.ProgramId = CompileShaders(program.VertexShaderSource, program.FragmentShaderSource);

                _shaderPrograms.Add(program.Name, program);

                i++; // Skip the next file since we processed a pair
            }
        }

        public static void Clear()
        {
            foreach (var program in _shaderPrograms.Values)
                GL.DeleteProgram(program.ProgramId);

            _shaderPrograms.Clear();
        }


        private static int CompileShaders(string vertexSource, string fragmentSource)
        {
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);

            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);

            GL.DetachShader(program, vertexShader);
            GL.DetachShader(program, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return program;
        }
    }
}
