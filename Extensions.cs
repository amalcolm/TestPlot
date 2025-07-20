using System.Runtime.InteropServices;

namespace TestPlot
{
    public static class Extensions
    {

        public static void Invoker(this Control control, Action action)
        {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }

        public static void SetText(this Label label, string text)
        {
            label.Invoker(() => label.Text = text);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto)] private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
        private const int WM_SETREDRAW = 0x000B;

        public static void SuspendDrawing(this Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, false, 0);
        }

        public static void ResumeDrawing(this Control control, bool refresh = true)
        {
            SendMessage(control.Handle, WM_SETREDRAW, true, 0);

            if (refresh)
                control.Refresh();
        }

    }
}