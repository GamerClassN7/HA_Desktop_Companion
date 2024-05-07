using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Sensors
{
    class IpLocation
    {
        public static string getData()
        {
            try
            {
                string info = new WebClient().DownloadString("http://ipinfo.io/");
                return (string)JObject.Parse(info)["loc"];
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
