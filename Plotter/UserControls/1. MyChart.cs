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

        private LabelAreaRenderer _labelAreaRenderer = default!;

        public MyChart()
            => InitializeComponent();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public MySerialIO IO { get; set; } = default!;

        private void MyChart_Load(object sender, EventArgs e)
        {
            IO = new();
            IO.FrameReceived += IO_FrameReceived;
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

        private void IO_FrameReceived(MySerialIO io, MyFrame frame)
        {
            if (TestMode) return;
            if (frame is not Text_Frame textFrame) return;

            var data = MyTextParser.Parse(textFrame.Text); if (data == null) return;

            foreach (var kvp in data)
            {
                if (Plots.TryGetValue(kvp.Key, out var plot) == false)
                {
                    plot = new MyPlot(WindowSize, this);
                    Plots[kvp.Key] = plot;
                    CreateTextBlocksForLabel(kvp.Key);
                }

                plot.Add(textFrame.Time, kvp.Value);
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
                    string newText = kvp.Value.ToString("F2");
                    if (tuple.Item2.Text != newText)
                    {
                        tuple.Item2.Text = newText; // This sets the 'dirty' flag inside the TextBlock
                    }
                    _textBlocksToRender.Add(tuple.Item1);
                    _textBlocksToRender.Add(tuple.Item2);
                }
            }

            if (!_textBlocksToRender.Any()) return;

            // 2. Calculate the total bounding box for all visible labels.
            RectangleF totalBounds = CalculateTotalBounds(_textBlocksToRender);

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
                _labelAreaRenderer.Render(paddedBounds, projection);
            }

            // 4. Render the text on top.
            // This call uses the fontRenderer from the base MyGLControl,
            // which correctly sets the text shader program before drawing.
            fontRenderer.RenderText(_textBlocksToRender);
        }

        // Add this helper method inside the MyChart class
        private RectangleF CalculateTotalBounds(List<TextBlock> blocks)
        {
            RectangleF totalBounds = RectangleF.Empty;

            foreach (var block in blocks)
            {
                // GetVertices ensures the bounds are recalculated if the block is dirty
                block.GetVertices();

                if (block.Bounds.IsEmpty) continue;

                if (totalBounds.IsEmpty)
                {
                    totalBounds = block.Bounds;
                }
                else
                {
                    totalBounds = RectangleF.Union(totalBounds, block.Bounds);
                }
            }
            return totalBounds;
        }
    }
}
