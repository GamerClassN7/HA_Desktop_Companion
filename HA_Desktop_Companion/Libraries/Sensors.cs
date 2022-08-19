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
    class Sensors
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
                        string outputResult = Regex.Replace(output[line + 1], @"\t|\n|\r", "").Trim();
                        return outputResult;
                    }

                }
            } catch (Exception) { }
            return "";
        }

        public static string queryWifi(string selector, string deselector = "")
        {
            var process = new Process
            {
                StartInfo = {
                        FileName = "netsh.exe",
                        Arguments = "wlan show interfaces",
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
                foreach (var item in process.StandardOutput.ReadToEnd().ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    string outputResult = Regex.Replace(item.Split(":")[1].Trim(), @"\t|\n|\r", "").Trim();
                    if (!String.IsNullOrEmpty(deselector))
                    {
                        if (item.Contains(selector) && !item.Contains(deselector))
                        {
                            return outputResult;
                        }
                    }
                    
                    if (item.Contains(selector))
                    {
                        return outputResult;
                    }
                }
            }
            catch (Exception) { }
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

        public static string queryLocationByIP()
        {
            try
            {
                string info = new WebClient().DownloadString("http://ipinfo.io/");
                return (string) JsonSerializer.Deserialize<JsonObject>(info)["loc"];
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool queryConsetStore(string category = "webcam")
        {
            string[] consentStores = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + category + @"\" ,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + category + @"\NonPackaged" 
            };

            foreach (string path in consentStores)
            {
                using (var rootKey = Registry.CurrentUser.OpenSubKey(path))
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
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    
        public static dynamic convertToType(dynamic variable)
        {
            //ADD double 
            string variableStr = variable.ToString();
            Debug.WriteLine("BEFORE CONVERSION" + variableStr);
            if (Regex.IsMatch(variableStr, "^(?:tru|fals)e$", RegexOptions.IgnoreCase)){
                Debug.WriteLine("AFTER CONVERSION (Bool)" + variableStr.ToString());
                return bool.Parse(variableStr);
            }
            else if (Regex.IsMatch(variableStr, @"^[0-9]+.[0-9]+$"))
            {
                Debug.WriteLine("AFTER CONVERSION (double)" + variableStr.ToString());
                return double.Parse(variableStr);
            }
            else if (Regex.IsMatch(variableStr, @"^\d$")) {
                Debug.WriteLine("AFTER CONVERSION (int)" + variableStr.ToString());
                return int.Parse(variableStr);
            }

            Debug.WriteLine("AFTER CONVERSION" + variableStr.ToString());
            return variableStr;
        }
    }
}
