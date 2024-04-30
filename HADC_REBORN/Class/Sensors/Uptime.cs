using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Sensors
{
    class Uptime
    {
        public static double GetValue()
        {
            try
            {
                [DllImport("kernel32")]
                extern static ulong GetTickCount64();
                return TimeSpan.FromMilliseconds(GetTickCount64()).TotalHours;
            }
            catch (Exception)
            {
            }

            return 0;
        }
    }
}
