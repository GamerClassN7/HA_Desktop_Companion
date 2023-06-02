using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;

namespace HA.Class.HomeAssistant.Objects
{
    public class HomeAssistatnDevice
    {
        public string device_id = null;
        public string app_id = null;
        public string app_name = null;
        public string app_version = null;
        public string device_name = null;
        public string manufacturer = null;
        public string model = null;
        public string os_name = null;
        public string os_version = null;
        public bool supports_encryption = true;

        public object? app_data = null;

        public void getDevice()
        {
            /*return new JObject()
            {
                ["device_id"] = device_id,
                ["app_id"] = app_id,
                ["app_name"] = app_name,
                ["app_version"] = app_version,
                ["device_name"] = device_name,
                ["manufacturer"] = manufacturer,
                ["model"] = model,
                ["os_name"] = os_name,
                ["os_version"] = os_version,
                ["supports_encryption"] = supports_encryption,
                ["app_data"] = app_data,
            };*/
        }
    }
}
