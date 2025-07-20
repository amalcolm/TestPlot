#pragma warning disable CS8765

using System.ComponentModel;
using Timer = System.Windows.Forms.Timer;

namespace Plotter
{

    public class MyTextbox : RichTextBox
    {
        private static readonly Timer _updateTimer = new();
        private static readonly HashSet<MyTextbox> _dirtyControls = [];
        private string _bufferedText = string.Empty;
        private bool _isUpdatingFromTimer = false;

        static MyTextbox()
        {
            _updateTimer.Interval = 50;
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        public MyTextbox()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

        }

        [Browsable(true)]
        public override string Text
        {
            get => _bufferedText;
            set
            {
                // If the timer is updating the text, don't re-process.
                if (_isUpdatingFromTimer) return;

                var newText = value ?? string.Empty;
                if (_bufferedText == newText) return;

                _bufferedText = newText;

                lock (_dirtyControls)
                    _dirtyControls.Add(this);
            }
        }

        private static void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_dirtyControls.Count == 0) return;

            List<MyTextbox> controlsToUpdate;
            lock (_dirtyControls)
            {
                controlsToUpdate = [.. _dirtyControls];
                _dirtyControls.Clear();
            }

            foreach (var tb in controlsToUpdate)
                if (!tb.IsDisposed)
                    tb.UpdateText(tb._bufferedText);
        }

        private void UpdateText(string text)
        {
            _isUpdatingFromTimer = true;
            base.Text = text;
            _isUpdatingFromTimer = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_dirtyControls)
                {
                    _dirtyControls.Remove(this);
                }
            }
            base.Dispose(disposing);
        }
    }

}