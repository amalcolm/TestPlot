namespace Plotter
{
    partial class MyOldChart
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            formsPlot = new ScottPlot.WinForms.FormsPlot();
            formView = new Plotter.MyFormView();
            SuspendLayout();
            // 
            // formsPlot
            // 
            formsPlot.BackColor = Color.Gainsboro;
            formsPlot.DisplayScale = 1F;
            formsPlot.Dock = DockStyle.Fill;
            formsPlot.Location = new Point(0, 0);
            formsPlot.Name = "formsPlot1";
            formsPlot.Size = new Size(734, 480);
            formsPlot.TabIndex = 0;
            // 
            // formView
            // 
            formView.BackColor = Color.Gainsboro;
            formView.Chart = null;
            formView.Dock = DockStyle.Right;
            formView.Font = new Font("Segoe UI", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            formView.Location = new Point(734, 0);
            formView.Margin = new Padding(5);
            formView.Name = "myFormView1";
            formView.Size = new Size(260, 480);
            formView.TabIndex = 1;
            // 
            // MyChart
            // 
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.Gainsboro;
            Controls.Add(formsPlot);
            Controls.Add(formView);
            Name = "MyChart";
            Size = new Size(994, 480);
//            Load += MyChart_Load;
            ResumeLayout(false);
        }

        #endregion

        private ScottPlot.WinForms.FormsPlot formsPlot;
        private Plotter.MyFormView formView;
    }
}
