using OpenTK.Graphics.OpenGL4;

namespace Plotter
{
    public class MyPlot : IDisposable
    {
        public Color Colour { get; set; } = MyColours.GetNextColour();

        private readonly object _lock = new();

        // OpenGL handles
        private readonly int _vboHandle; // Vertex Buffer Object
        private readonly int _vaoHandle; // Vertex Array Object

        // Configuration
        private readonly int _maxVertices;
        private readonly int _windowSize;

        // Data and state
        private readonly float[] _vertexData; // The C# circular buffer for vertices (x,y,z)
        private int _currentIndex = 0;        // The index for the next data point
        private double _xCounter = -Math.Pow(2,22)-1; // maintain float precision for X values
        private int _totalPoints = 0;         // Total points currently in the buffer

        public RunningAverage runningAverage { get; set; }
        public double XCounter => _xCounter;
        public int WindowSize => _windowSize;

        /// <summary>
        /// Creates a new plot object with its own GPU buffers.
        /// </summary>
        /// <param name="windowSize">The number of most recent vertices to draw.</param>
        public MyPlot(int windowSize)
        {
            runningAverage = new(windowSize);
            _maxVertices = windowSize * 4 + Random.Shared.Next(windowSize / 10);  // stagger the block copies to avoid synchronization issues
            _windowSize = windowSize;

            // Allocate the C# array to hold all vertex data. 3 floats per vertex (x, y, z).
            _vertexData = new float[_maxVertices * 3];

            // --- One-time OpenGL Setup ---
            // 1. Create and bind a VAO
            _vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(_vaoHandle);

            // 2. Create a VBO and allocate memory on the GPU
            _vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboHandle);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                _vertexData.Length * sizeof(float),
                IntPtr.Zero, // Allocate memory, but don't upload data yet
                BufferUsageHint.DynamicDraw // Hint that we will be updating this buffer frequently
            );

            // 3. Configure vertex attributes
            // Tell OpenGL that our vertex data is arranged as 3 floats per vertex.
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Unbind the VAO to prevent accidental changes
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// Adds a new Y data point to the plot. The X value is automatically incremented.
        /// </summary>
        public void Add(double y)
        {
            lock (_lock)
            {
                runningAverage.Add(y);

                _vertexData[_currentIndex * 3 + 0] = (float)_xCounter;
                _vertexData[_currentIndex * 3 + 1] = (float)y;
                _vertexData[_currentIndex * 3 + 2] = 0.0f;

                _xCounter++;
                if (_totalPoints < _maxVertices)
                    _totalPoints++;

                _currentIndex++;
                
                if (_currentIndex >= _maxVertices)
                {
                    int sourceIndex = (_maxVertices - WindowSize) * 3;
                    int length = WindowSize * 3;

                    Array.Copy(_vertexData, sourceIndex, _vertexData, 0, length);
                    // had planned to move the X vlues toward zero for accuracy, but doesn't seem necessary
                    // circular buffer works if we copy the last value to the front, and draw two line strips

                    _currentIndex = WindowSize;
                }
            }
        }

        /// <summary>
        /// Renders the plot. Assumes the correct shader program is already active.
        /// </summary>
        public void Render()
        {
            if (_vaoHandle == 0 || _vboHandle == 0 || _totalPoints < 2) return;

            lock (_lock)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vboHandle);
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertexData.Length * sizeof(float), _vertexData);

                GL.BindVertexArray(_vaoHandle);

                int pointsToDraw = Math.Min(_totalPoints, _windowSize);
                int startIdx = _currentIndex - pointsToDraw;

                GL.DrawArrays(PrimitiveType.LineStrip, startIdx, pointsToDraw);
            }
        }

        /// <summary>
        /// Releases the GPU resources (VBO and VAO).
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_vboHandle != 0) GL.DeleteBuffer(_vboHandle);
                if (_vaoHandle != 0) GL.DeleteVertexArray(_vaoHandle);
            }
            GC.SuppressFinalize(this);
        }
    }
}