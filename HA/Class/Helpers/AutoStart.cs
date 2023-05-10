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
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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
