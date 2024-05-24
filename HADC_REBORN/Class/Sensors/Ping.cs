using HADC_REBORN.Class.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Sensors
{
    class Ping
    {
        public static bool GetValue(string host)
        {
            return Network.PingHost(host);
        }
    }
}
