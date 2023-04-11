using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HA.Class.Sensors
{
    internal class CurrentWindow
    {
        public static string GetValue()
        {
            try
            {
                [DllImport("user32.dll")]
                static extern IntPtr GetForegroundWindow();

                [DllImport("user32.dll")]
                static extern int GetWindowText(IntPtr hwnd, StringBuilder ss, int count);

                IntPtr handle = IntPtr.Zero;
                handle = GetForegroundWindow();

                const int nChar = 256;
                StringBuilder ss = new StringBuilder(nChar);

                ss = new StringBuilder(nChar);
                string fullName = "";
                if (GetWindowText(handle, ss, nChar) > 0)
                    fullName = ss.ToString();

                return fullName;
            }
            catch (Exception ex) { }
            return "";
        }
    }
}
