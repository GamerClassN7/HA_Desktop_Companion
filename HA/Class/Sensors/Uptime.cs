using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace HA.Class.Sensors
{
    class Uptime
    {
        public static double GetValue()
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
    }
}
