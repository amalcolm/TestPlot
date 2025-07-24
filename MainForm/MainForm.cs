namespace TestPlot
{
    public partial class MainForm : Form
    {
        internal bool isClosing = false;

        public MainForm()
            => InitializeComponent();

        private void MainForm_Load(object sender, EventArgs e)
            => FindSerial();
        

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
            => isClosing = true;


        public void _LOG_ERROR(string message)
        {
            if (isClosing) return;

            this.Invoker(() => labError.Text = message);
        }

    }
}
