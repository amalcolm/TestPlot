using System;
using System.Windows.Forms;

namespace CTG_Comms
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
    }
}