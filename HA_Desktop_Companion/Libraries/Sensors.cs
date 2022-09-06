using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        public static string queryWmic(string wmic_path, string wmic_selector, string wmic_namespace = @"\\root\wmi")
        {
            var process = new Process
            {
                StartInfo = {
                    FileName = "wmic.exe",
                    Arguments = ("/namespace:\"" + wmic_namespace + "\" path " + wmic_path + " get " + wmic_selector),
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
                    if (output[line].Contains(wmic_selector))
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
            try 
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
                process.WaitForExit();

                foreach (var item in output)
                {
                    if (item.Split(":").Count() < 2)
                        continue;

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
            catch (Exception ex) {}
            return "";
        }

        public static object queryCurrentWindow()
        {
            try
            {
                [DllImport("user32.dll")]
                static extern IntPtr GetForegroundWindow();

                [DllImport("user32.dll")]
                static extern int GetWindowText(IntPtr hwnd, StringBuilder ss, int count);

                [DllImport("user32.dll")]
                static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

                IntPtr handle = IntPtr.Zero;
                handle = GetForegroundWindow();

                const int nChar = 256;
                StringBuilder ss = new StringBuilder(nChar);

                /*uint procesId; 
                GetWindowThreadProcessId(handle, out procesId);
                return Process.GetProcessById((int)procesId).ProcessName + ".exe";*/

                /*if  ( <= 0)
                    return "";

                MessageBox.Show(ss.ToString());*/

                /*string path = ss.ToString();
                string exeName = path.Split("\\").Last();
                */

                //TODO: Handle like Attributes for full name and path
                ss = new StringBuilder(nChar);
                string fullName = "";
                if (GetWindowText(handle, ss, nChar) > 0)
                    fullName = ss.ToString();

                return fullName;
            }
            catch (Exception ex) { }
            return "";
        }

        public static double queryUptime()
        {
            try
            {
                [DllImport("kernel32")]
                extern static UInt64 GetTickCount64();
                return (TimeSpan.FromMilliseconds(GetTickCount64())).TotalHours;
            }
            catch (Exception)
            {
            }
            return 0;
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

        public static bool queryConsentStore(string consent_category)
        {
            try
            {
                string[] consentStores = {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + consent_category + @"\" ,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + consent_category + @"\NonPackaged" 
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

                                        var endTime = (long)subKey.GetValue("LastUsedTimeStop");
                                        Debug.WriteLine(consent_category + " " + subKey.GetValue("LastUsedTimeStop"));

                                        if (endTime > 0)
                                        {
                                            //MessageBox.Show(subKey.GetValue("LastUsedTimeStop").ToString());
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception){}

            return false;
        }

        public static dynamic convertToType(dynamic variable)
        {
            //ADD double 
            string variableStr = variable.ToString();
            //Debug.WriteLine("BEFORE CONVERSION" + variableStr);
            if (Regex.IsMatch(variableStr, "^(?:tru|fals)e$", RegexOptions.IgnoreCase)){
                //Debug.WriteLine("AFTER CONVERSION (Bool)" + variableStr.ToString());
                return bool.Parse(variableStr);
            }
            else if (Regex.IsMatch(variableStr, @"^[0-9]+.[0-9]+$"))
            {
                //Debug.WriteLine("AFTER CONVERSION (double)" + variableStr.ToString());
                return double.Parse(variableStr);
            }
            else if (Regex.IsMatch(variableStr, @"^\d$")) {
                //Debug.WriteLine("AFTER CONVERSION (int)" + variableStr.ToString());
                return int.Parse(variableStr);
            }

            //Debug.WriteLine("AFTER CONVERSION" + variableStr.ToString());
            return variableStr;
        }

        public static bool queryRestartPending()
        {
            using (var rootKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending"))
            {
                if (rootKey != null)
                {
                    if (rootKey.GetSubKeyNames().Length > 0)
                        return true;
                }
            }

            using (var rootKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
            {
                if (rootKey != null)
                {
                    if (rootKey.GetSubKeyNames().Length > 0)
                        return true;
                }
            }

            using (var rootKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager"))
            {
                if (rootKey != null)
                {
                    if (rootKey.GetValueKind("PendingFileRenameOperations") != null)
                        return true;
                }
            }

            return false;
        }
    }
}
