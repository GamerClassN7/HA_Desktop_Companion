using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace HA_Desktop_Companion.Libraries
{
    class Senzors
    {
        public static string queryWMIC(string path, string selector, string wmiNmaespace = @"\\root\wmi")
        {
            var process = new Process
            {
                StartInfo = {
                    FileName = "wmic.exe",
                    Arguments = ("/namespace:\"" + wmiNmaespace + "\" path " + path + " get " + selector),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string[] output = process.StandardOutput.ReadToEnd().ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            process.Dispose();

            try
            {
                for (int line = 0; line < output.Length; line++)
                {
                    if (output[line].Contains(selector))
                    {
                        Regex.Replace(output[line + 1], @"\t|\n|\r", "");
                        return output[line + 1];
                    }

                }
            } catch (Exception) { }
            return "";
        }

        public static string queryActiveWindowTitle()
        {
            [DllImport("user32.dll")]
            static extern IntPtr GetForegroundWindow();
            [DllImport("user32.dll")]
            static extern int GetWindowText(IntPtr hwnd, StringBuilder ss, int count);

            const int nChar = 256;
            StringBuilder ss = new StringBuilder(nChar);

            IntPtr handle = IntPtr.Zero;
            handle = GetForegroundWindow();

            if (GetWindowText(handle, ss, nChar) > 0) return ss.ToString();
            else return "";
        }

        public static TimeSpan queryMachineUpTime()
        {
            [DllImport("kernel32")]
            extern static UInt64 GetTickCount64();
            return (TimeSpan.FromMilliseconds(GetTickCount64()));
        }
    }
}
