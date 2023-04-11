using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HA.Class.Sensors
{
    internal class IpLocation
    {
        public static string getData()
        {
            try
            {
                string info = new WebClient().DownloadString("http://ipinfo.io/");
                return (string)JsonSerializer.Deserialize<JsonObject>(info)["loc"];
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}
