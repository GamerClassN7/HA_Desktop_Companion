using System;
using System.Net;
using Newtonsoft.Json.Linq;

namespace HA.Class.Sensors
{
    internal class IpLocation
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
