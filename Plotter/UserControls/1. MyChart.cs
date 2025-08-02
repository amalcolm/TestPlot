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

        private bool _labelsAreDirty = true;
        protected override void DrawText()
        {
            if (font == null) return;
            
            _textBlocksToRender.Clear();

            foreach (var kvp in _latestValues)
                if (_blocks.TryGetValue(kvp.Key, out var tuple))
                {
                    var labelBlock = tuple.Item1;
                    var valueBlock = tuple.Item2;

                    valueBlock.Text = kvp.Value.ToString("F2");

                    _textBlocksToRender.Add(valueBlock);
                    _textBlocksToRender.Add(labelBlock);
                }

            _labelAreaRenderer.Render(_textBlocksToRender, font);
        }
    }
}
