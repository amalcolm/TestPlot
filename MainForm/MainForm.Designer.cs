﻿
namespace TestPlot
{
    partial class MainForm : Form
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
            labError = new Label();
            myChart = new Plotter.MyChart();
            SuspendLayout();
            // 
            // cbPorts
            // 
            cbPorts.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            cbPorts.FormattingEnabled = true;
            cbPorts.Location = new Point(31, 12);
            cbPorts.Name = "cbPorts";
            cbPorts.Size = new Size(150, 33);
            cbPorts.TabIndex = 1;
            cbPorts.SelectedIndexChanged += cbPorts_SelectedIndexChanged;
            // 
            // labError
            // 
            labError.Font = new Font("Segoe UI", 16F);
            labError.ForeColor = Color.Firebrick;
            labError.Location = new Point(261, 12);
            labError.Name = "labError";
            labError.Size = new Size(999, 33);
            labError.TabIndex = 5;
            labError.Text = "Plotter v1.00";
            labError.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // myChart
            // 
            myChart.Location = new Point(18, 66);
            myChart.Name = "myChart";
            myChart.Size = new Size(1680, 777);
            myChart.TabIndex = 6;
            // 
            // MainForm
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(1720, 855);
            Controls.Add(myChart);
            Controls.Add(labError);
            Controls.Add(cbPorts);
            DoubleBuffered = true;
            Name = "MainForm";
            Text = "MainForm";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            ResumeLayout(false);
        }

        #endregion
        private ComboBox cbPorts;
        private Label labError;
        private Plotter.MyChart myChart;
    }
}