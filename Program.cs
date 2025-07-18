namespace TestPlot
{
    internal static class Program
    {
        public static bool IsRunning { get; set; } = false;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            IsRunning = true;
            Application.Run(new MainForm());
        }
    }
}