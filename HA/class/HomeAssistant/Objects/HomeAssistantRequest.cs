using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace HA.Class.HomeAssistant.Objects
{
    internal class HomeAssistantRequest
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
