
namespace TestPlot
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            cbPorts = new ComboBox();
            tbComms = new TextBox();
            myChart1 = new Plotter.MyChart();
            SuspendLayout();
            // 
            // cbPorts
            // 
            cbPorts.FormattingEnabled = true;
            cbPorts.Location = new Point(31, 12);
            cbPorts.Name = "cbPorts";
            cbPorts.Size = new Size(121, 23);
            cbPorts.TabIndex = 1;
            // 
            // tbComms
            // 
            tbComms.Location = new Point(1368, 56);
            tbComms.Multiline = true;
            tbComms.Name = "tbComms";
            tbComms.Size = new Size(340, 737);
            tbComms.TabIndex = 2;
            // 
            // myChart1
            // 
            myChart1.BackColor = Color.Transparent;
            myChart1.IO = null;
            myChart1.IsWarning = false;
            myChart1.Location = new Point(36, 128);
            myChart1.Name = "myChart1";
            myChart1.Size = new Size(1200, 642);
            myChart1.TabIndex = 3;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1720, 836);
            Controls.Add(myChart1);
            Controls.Add(tbComms);
            Controls.Add(cbPorts);
            Name = "MainForm";
            Text = "MainForm";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox cbPorts;
        private TextBox tbComms;
        private Plotter.MyChart myChart1;
    }
}