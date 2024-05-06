using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Xml.Linq;

namespace HADC_REBORN.Class.Sensors
{
    class Wmic
    {
        private static Dictionary<string,string[]> wmicClasses = new Dictionary<string, string[]>();

        private static string[] getClasses(string wmic_namespace = @"root\\wmi")
        {
            if (!Wmic.wmicClasses.ContainsKey(wmic_namespace))
            {
                Wmic.wmicClasses[wmic_namespace] = new string[] { };
            }

            if (Wmic.wmicClasses[wmic_namespace].Length > 0)
            {
                return Wmic.wmicClasses[wmic_namespace];
            }

            ManagementClass nsClass = new ManagementClass(new ManagementScope(wmic_namespace), new ManagementPath("__namespace"), null);
            string[] classList = new string[] { };

            foreach (ManagementObject ns in nsClass.GetInstances())
            {
                if (ns["Name"] == null)
                {
                    continue;
                }

                Array.Resize(ref classList, classList.Length + 1);
                classList[(classList.Length - 1)] = (string) ns["Name"];
            }

            Wmic.wmicClasses[wmic_namespace] = classList;
            App.log.writeLine(String.Join(",", classList));

            return Wmic.wmicClasses[wmic_namespace];
        }

        public static string GetValue(string wmic_class, string wmic_selector, string wmic_namespace = @"root\\wmi", int wmic_iterator_index = 0)
        {
            string result = getClasses(wmic_namespace).FirstOrDefault(x => x == wmic_class);
            if (result == null)
            {
                App.log.writeLine("Wmic Class '" + wmic_class + "' not found! in namespace " + wmic_namespace);
                return "";
            }

            App.log.writeLine("NAMESPACE " + wmic_namespace);
            App.log.writeLine("SELECT " + wmic_selector + " FROM " + wmic_class + "[" + wmic_iterator_index + "]");
            App.log.writeLine("ITERATOR " + wmic_iterator_index);

            ManagementScope scope = new ManagementScope(wmic_namespace);
            scope.Connect();
            
            WqlObjectQuery query = new WqlObjectQuery(("SELECT " + wmic_selector + " FROM " + wmic_class));

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query, null);
            int i = 0;

            try
            {
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
            catch (ManagementException e)
            {
                App.log.writeLine("ERROR:  " + e.Message);
            }

            scope.Clone();
            return "";
        }
    }
}
