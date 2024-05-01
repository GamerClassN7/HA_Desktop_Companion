using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.HomeAssistant.Objects
{
    public class ApiSensor
    {
        public string device_class = null;
        public string icon = "mdi:battery";
        public string name = null;
        public dynamic state = null;
        public string type = "sensor";
        public string unique_id = null;
        public string unit_of_measurement = null;
        public string state_class = null;
        public string entity_category = null;
        public bool? disabled = null;
    }
}
