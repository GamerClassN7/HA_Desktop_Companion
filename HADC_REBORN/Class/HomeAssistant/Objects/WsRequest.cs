using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.HomeAssistant.Objects
{
    internal class WsRequest
    {
        public int id = 1;
        public string type = "";
        public string webhook_id = null;
        public bool support_confirm = false;
    }
}
