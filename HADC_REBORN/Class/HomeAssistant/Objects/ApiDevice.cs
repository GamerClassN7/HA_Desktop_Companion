using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.HomeAssistant.Objects
{
    class ApiDevice
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

    }
}
