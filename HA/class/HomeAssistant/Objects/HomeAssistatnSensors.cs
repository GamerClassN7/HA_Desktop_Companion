﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using Newtonsoft.Json.Linq;

namespace HA.Class.HomeAssistant.Objects
{
    public class HomeAssistatnSensors
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
