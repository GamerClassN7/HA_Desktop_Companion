using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace HA.Class.Helpers
{
    public class AutoStart
    {
    

        public static void register()
        {
            try
            {
                // Computer\HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    #if DEBUG
                        string exeFullName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        string assemblyFolder = System.IO.Path.GetDirectoryName(exeFullName);
                    #else
                         string assemblyFolder = AppDomain.CurrentDomain.BaseDirectory;
                    #endif

                    string exePath = Path.Combine(assemblyFolder, System.AppDomain.CurrentDomain.FriendlyName + ".exe");
                    key.SetValue(System.AppDomain.CurrentDomain.FriendlyName, "\"" + exePath + "\"");
                    key.Close();
                }
            }
            catch (Exception)
            {
                throw new Exception("Autostart Registration Failed!");
            }

        }
    }
}
