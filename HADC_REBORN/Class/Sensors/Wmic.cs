using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace HADC_REBORN.Class.Sensors
{
    class Wmic
    {
        public static string GetValue(string wmic_class, string wmic_selector, string wmic_namespace = @"root\\wmi", int wmic_iterator_index = 0)
        {
            App.log.writeLine("NAMESPACE " + wmic_namespace);
            App.log.writeLine("SELECT " + wmic_selector + " FROM " + wmic_class + "[" + wmic_iterator_index + "]");
            App.log.writeLine("ITERATOR " + wmic_iterator_index);

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmic_namespace, ("SELECT " + wmic_selector + " FROM " + wmic_class));
                int i = 0;

                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (!Equals(queryObj, null) && queryObj != null && queryObj != new ManagementObject() { } && wmic_iterator_index == i)
                    {
                        if (queryObj.Properties.Count > 0 && queryObj?[wmic_selector]?.ToString() != null) //TODO: Eary Return
                        {
                            App.log.writeLine("OUTPUT: " + queryObj?[wmic_selector]?.ToString());
                            return queryObj?[wmic_selector]?.ToString();
                        }
                    }

                    i++;
                }
            }
            catch (Exception e)
            {

            }
            return "";
        }
    }
}
