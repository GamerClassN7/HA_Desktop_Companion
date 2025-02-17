using System.Diagnostics;
using HADC_REBORN.Class.Helpers;

namespace HADC_REBORN.Class.Sensors
{
    internal class LockedState
    {
        public static bool GetValue()
        {
            // Get the list of all processes named "LogonUI"
            Process[] processes = Process.GetProcessesByName("LogonUI");

            // Return true if the process is found, otherwise false
            return processes.Length > 0;
        }
    }
}
