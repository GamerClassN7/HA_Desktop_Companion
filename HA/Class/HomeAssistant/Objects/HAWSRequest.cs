using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HA.Class.HomeAssistant.Objects
{
    internal class HAWSRequest
    {
        public int id = 1;
        public string type = "";
        public string webhook_id = "";
        public bool support_confirm = false;
    }
}
