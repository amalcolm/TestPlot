

namespace TestPlot
{
    public partial class MainForm : Form
    {
        bool isClosing = false;

        public MainForm()
        {
            InitializeComponent();
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            isClosing = true;
        }

        public void _LOG_ERROR(string message)
        {
            if (isClosing) return;
            if (InvokeRequired)
                this.Invoker(() => tbComms.AppendText(message));
            else
                tbComms.AppendText(message);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            FindSerial();
        }
    }
}
