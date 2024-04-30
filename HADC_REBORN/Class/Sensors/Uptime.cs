using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.HomeAssistant.Sensors
{
    class Uptime
    {
        public static double GetValue()
        {
            try 
            { 
                [DllImport("kernel32")]
                extern static UInt64 GetTickCount64();
                return TimeSpan.FromMilliseconds(GetTickCount64()).TotalHours;
            } 
            catch (Exception) 
            {
            }     

            return 0;
        }
    }
}
