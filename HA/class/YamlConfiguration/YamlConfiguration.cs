using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HA.@class.YamlConfiguration
{
    internal class YamlConfiguration
    {
        private string path = null;
        private object configurationData = null;
        public YamlConfiguration(string configurationFilePath) {
            path = configurationFilePath;
        }
    }
}
