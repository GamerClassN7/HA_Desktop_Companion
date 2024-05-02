using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Sensors
{
    class NetworkInterface
    {
        public static string GetValue(string selector, string deselector = "")
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
                    string[] line = item.Split(":");
                    if (!(line.Length < 2))
                    {
                        continue;
                    }

                    string outputResult = Regex.Replace(line[1].Trim(), @"\t|\n|\r", "").Trim();

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

            return "Unknown";
        }
    }
}
