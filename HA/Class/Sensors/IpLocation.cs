using System;
using System.Net;
using System.Runtime.InteropServices;
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
        public static void test()
        {
            [DllImport("user32.dll")]
            static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

            keybd_event((byte)173, 0, 0, 0); // increase volume

        }
    }

    
}
