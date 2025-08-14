using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Plotter.Backgrounds;
using Plotter.Fonts;
using System.ComponentModel;

namespace Plotter.UserControls
{
    internal partial class MyChart : MyPlotter
    {
        private const int WindowSize = 1200;

        private readonly Dictionary<string, double> _latestValues = [];
        private readonly Dictionary<string, Tuple<TextBlock, TextBlock>> _blocks = [];
        private readonly List<TextBlock> _textBlocksToRender = [];

        private LabelAreaRenderer? _labelAreaRenderer;

        public MyChart()
            => InitializeComponent();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MySerialIO IO { get; set; } = default!;

        private void MyChart_Load(object sender, EventArgs e)
        {
            IO = new();
            IO.TextReceived += IO_TextReceived;
//            IO.FrameReceived += IO_FrameReceived;

        }

        protected override void Init()
        {
            base.Init();
            _labelAreaRenderer = new (this, "Resources/Backgrounds/LabelArea.png");
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            _labelAreaRenderer?.Shutdown();
        }

        Dictionary<string, double> _data = [];
        private void IO_TextReceived(MySerialIO _, AString line)
        {
            MyTextParser.Parse(line, _data); if (_data.Count == 0) return;
            foreach (var kvp in _data)
            {
                if (Plots.TryGetValue(kvp.Key, out var plot) == false)
                {
                    plot = new MyPlot(WindowSize, this);
                    Plots[kvp.Key] = plot;
                    CreateTextBlocksForLabel(kvp.Key);
                }
                plot.Add(line.Time, kvp.Value);
                _latestValues[kvp.Key] = kvp.Value;
            }
        }
        

        private void CreateTextBlocksForLabel(string label)
        {
            if (font == null) return;

            string labelText = $": {label.Trim()}";

            float yPos = MyGL.Height - 70 - (_blocks.Count*50);

            var labelBlock = new TextBlock(labelText, 106, yPos, font);
            var valueBlock = new TextBlock("0.00", 100 , yPos, font, TextAlign.Right);
            
            _blocks[label] = Tuple.Create(labelBlock, valueBlock);
        }

        protected override void DrawText()
        {
            if (font == null) return;

            _textBlocksToRender.Clear();
            // 1. Populate the list of blocks to render and flag if their content has changed.
            foreach (var kvp in _latestValues)
            {
                if (_blocks.TryGetValue(kvp.Key, out var tuple))
                {
                    tuple.Item2.SetValue(kvp.Value, "F2");

                    _textBlocksToRender.Add(tuple.Item1);
                    _textBlocksToRender.Add(tuple.Item2);
                }
            }

            if (!_textBlocksToRender.Any()) return;
            
            // 2. Calculate the total bounding box for all visible labels.
            RectangleF totalBounds = CalculateTotalBounds();

            // 3. Render the background with padding.
            if (!totalBounds.IsEmpty)
            {
                float padding = 10f;
                var paddedBounds = new RectangleF(
                    totalBounds.X - padding,
                    totalBounds.Y - padding,
                    totalBounds.Width + (padding * 2),
                    totalBounds.Height + (padding * 2)
                );
                var projection = Matrix4.CreateOrthographicOffCenter(0, MyGL.ClientSize.Width, 0, MyGL.ClientSize.Height, -1.0f, 1.0f);
                
                _labelAreaRenderer?.Render(paddedBounds, projection);

                GL.UseProgram(_textShaderProgram);
            }

            fontRenderer.RenderText(_textBlocksToRender);


        }

        // Return this helper method inside the MyChart class
        private RectangleF CalculateTotalBounds()
        {
            RectangleF totalBounds = RectangleF.Empty;

            foreach (var block in _textBlocksToRender)
            {
                if (block.Bounds.IsEmpty) continue;

                if (totalBounds.IsEmpty)
                    totalBounds = block.Bounds;
                else
                    totalBounds = RectangleF.Union(totalBounds, block.Bounds);
            }
        
            return totalBounds;
        }

        // Add cleanup logic when plots are removed to prevent memory leaks from the pool.
        private void RemovePlot(string key) // Example of a cleanup method
        {
            if (Plots.Remove(key, out var plot))
            {
                plot.Shutdown(); // Release OpenGL resources
            }

            if (_blocks.Remove(key, out var textBlocks))
            {
                textBlocks.Item1.Dispose(); // Return label buffer to pool
                textBlocks.Item2.Dispose(); // Return value buffer to pool
            }

            _latestValues.Remove(key);
        }

    }
}
