using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;

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

        /*public static string queryLocationByIP()
        {
            try
            {
                string info = new WebClient().DownloadString("http://ipinfo.io/");
                Debug.WriteLine(JsonSerializer.Deserialize<JsonObject>(info)["loc"]);
                return (string) JsonSerializer.Deserialize<JsonObject>(info)["loc"];
            }
            catch (Exception)
            {
                return null;
            }
        }*/

        public static bool queryWebCamUseStatus()
        {
            using (var rootKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam\"))
            {
                if (rootKey != null)
                {
                    foreach (var subKeyName in rootKey.GetSubKeyNames())
                    {
                        using (var subKey = rootKey.OpenSubKey(subKeyName))
                        {
                            if (subKey.GetValueNames().Contains("LastUsedTimeStop"))
                            {
                                var endTime = subKey.GetValue("LastUsedTimeStop") is long ? (long)subKey.GetValue("LastUsedTimeStop") : -1;
                                if (endTime == 0)
                                {
                                    MessageBox.Show(subKeyName);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
