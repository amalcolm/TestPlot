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


        [Browsable(true)] [Category("Appearance")] [Description("The text string from the serial device.")]
        public override string Text
        { 
            get => _text;
            set
            {
                if (_text == value) return;
                _text = value;

                foreach (string part in _text.Split(['\t'], StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] keyValue = part.Split([':'], 2, StringSplitOptions.RemoveEmptyEntries);

                    string fieldName = $"{keyValue[0].Trim()}:";
                    int    fieldHash = fieldName.GetHashCode();
                    string fieldValue = keyValue.Length > 1 ? keyValue[1].Trim() : string.Empty;

                    MyTextbox? myTB = _data.Keys.FirstOrDefault(k => k.Tag as int? == fieldHash);
                    if (myTB != null)
                    {
                        myTB.Text = fieldValue;
                    }
                    else
                    {
                        int y = _data.Count * 40 + 10;

                        myTB = new MyTextbox {
                            Tag = fieldHash,
                            Text = fieldValue,
                            Location = new Point(160, y), 
                            Size = new Size(100, 28),
                            BorderStyle = BorderStyle.None,
                            BackColor = Color.Gainsboro,
                        };
                        _data.Add(myTB, keyValue[0]);

                        Controls.Add(new Label()
                        {
                            Text = fieldName,
                            Location = new Point(10, y),
                            Size = new Size(150, 28),
                            TextAlign = ContentAlignment.MiddleRight,
                        });
                        Controls.Add(myTB);
                    }

                    if (double.TryParse(fieldValue, out double v) && Chart != null)
                        Chart.AddData(fieldName, fieldHash, v);
               }
            }
        }
        private string _text = string.Empty;

        private Dictionary<MyTextbox, string> _data = [];
    }
}
