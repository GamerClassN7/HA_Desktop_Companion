﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.HomeAssistant.Objects
{
    class ApiRequest
    {
        public string type = "";
        public object data = new object();

        public void SetData(object payloadData)
        {
            data = payloadData;
        }

        public void SetType(string payloadType)
        {
            type = payloadType;
        }
    }
}
