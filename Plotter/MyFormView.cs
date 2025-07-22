#pragma warning disable CS8765

using System.ComponentModel;

namespace Plotter
{
    public partial class MyFormView : UserControl
    {
        public MyFormView()
        {
            InitializeComponent();
        }

        [Browsable(true)] [Category("Appearance")] [Description("The chart to display data from the serial device.")]
        public MyChart? Chart { get; set; }


        private Dictionary<MyTextbox, string> _data = [];

        [Browsable(true)]
        [Category("Appearance")]
        [Description("The text string from the serial device.")]
        public override string Text
        {
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;

                // Get a ReadOnlySpan from the input string to avoid allocations
                ReadOnlySpan<char> textSpan = _text.AsSpan();

                // Loop through tab-separated parts without using Split()
                while (true)
                {
                    int tabIndex = textSpan.IndexOf('\t');

                    // Get the part before the tab, or the rest of the string if no tab is found
                    ReadOnlySpan<char> part = (tabIndex == -1) ? textSpan : textSpan.Slice(0, tabIndex);

                    // -- Process the key-value pair from the 'part' span --
                    int colonIndex = part.IndexOf(':');
                    if (colonIndex != -1)
                    {
                        ReadOnlySpan<char> keySpan = part.Slice(0, colonIndex);
                        ReadOnlySpan<char> valueSpan = part.Slice(colonIndex + 1);

                        // Reconstruct the field name to get the hash.
                        // This is one of the few remaining small allocations.
                        string fieldName = $"{keySpan}:";
                        int fieldHash = fieldName.GetHashCode();
                        string fieldValue = valueSpan.ToString();

                        MyTextbox? myTB = _data.Keys.FirstOrDefault(k => k.Tag as int? == fieldHash);
                        if (myTB != null)
                        {
                            myTB.Text = fieldValue;
                        }
                        else
                        {
                            int y = _data.Count * 40 + 10;
                            myTB = new MyTextbox
                            {
                                Tag = fieldHash,
                                Text = fieldValue,
                                Location = new Point(160, y),
                                Size = new Size(100, 28),
                                BorderStyle = BorderStyle.None,
                                BackColor = Color.Gainsboro,
                            };
                            _data.Add(myTB, keySpan.ToString());

                            Controls.Add(new Label()
                            {
                                Text = fieldName,
                                Location = new Point(10, y),
                                Size = new Size(150, 28),
                                TextAlign = ContentAlignment.MiddleRight,
                            });
                            Controls.Add(myTB);
                        }

                        if (double.TryParse(valueSpan, out double v) && Chart != null)
                        {
//                            Chart.AddData(fieldName, fieldHash, v);
                        }
                    }
                    // ----------------------------------------------------

                    if (tabIndex == -1) break; // Exit loop if no more tabs

                    // Move the span past the part we just processed
                    textSpan = textSpan.Slice(tabIndex + 1);
                }
            }
        }
        private string _text = string.Empty;

    }
}
