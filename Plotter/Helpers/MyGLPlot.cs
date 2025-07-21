using OpenTK.Graphics.OpenGL4;

namespace Plotter
{
    public class MyGLPlot : IDisposable
    {
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
        private double _xCounter = 0;           // A simple counter for the X-axis value
        private int _totalPoints = 0;         // Total points currently in the buffer

        public double XCounter => _xCounter;
        public int WindowSize => _windowSize;

        /// <summary>
        /// Creates a new plot object with its own GPU buffers.
        /// </summary>
        /// <param name="maxVertices">The maximum number of vertices to store in the circular buffer.</param>
        /// <param name="windowSize">The number of most recent vertices to draw.</param>
        public MyGLPlot(int maxVertices, int windowSize)
        {                                                       if (windowSize > maxVertices) throw new ArgumentException("windowSize cannot be larger than maxVertices.");
            _maxVertices = maxVertices;
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
                // Write the new vertex (x, y, z) into our C# circular buffer
                _vertexData[_currentIndex * 3 + 0] = (float)_xCounter;
                _vertexData[_currentIndex * 3 + 1] = (float)y;
                _vertexData[_currentIndex * 3 + 2] = 0.0f;

                // Advance the index and wrap around if necessary
                _currentIndex = ++_currentIndex % _maxVertices;

                // Increment our counters
                _xCounter += 0.01;

                if (_totalPoints < _maxVertices)
                    _totalPoints++;
            }
        }

        /// <summary>
        /// Renders the plot. Assumes the correct shader program is already active.
        /// </summary>
        public void Render()
        {
            lock (_lock)
            {
            
            // 1. Upload the latest vertex data from our C# array to the GPU buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboHandle);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertexData.Length * sizeof(float), _vertexData);

            // 2. Bind our VAO to activate the buffer configuration
            GL.BindVertexArray(_vaoHandle);

            // 3. Calculate which part of the circular buffer to draw
            int pointsToDraw = Math.Min(_totalPoints, _windowSize);
            if (pointsToDraw < 2) return; // Can't draw a line with less than 2 points

            // Calculate the starting index in our circular buffer
            int startIdx = (_currentIndex - pointsToDraw + _maxVertices) % _maxVertices;

                // 4. Draw the arrays. This may require two separate draw calls if the window wraps around the buffer.
                if (startIdx < _currentIndex)
                    GL.DrawArrays(PrimitiveType.LineStrip, startIdx, pointsToDraw           );
                else
                {
                    GL.DrawArrays(PrimitiveType.LineStrip, startIdx, _maxVertices - startIdx);
                    GL.DrawArrays(PrimitiveType.LineStrip, 0       , _currentIndex          );
                }
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