using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace HADC_REBORN.Class.Sensors
{
    class Wmic
    {
        // Connecting to a namespace and checking a class is expensive, sensors are queried every few seconds
        private static ConcurrentDictionary<string, ManagementScope> scopes = new ConcurrentDictionary<string, ManagementScope>(StringComparer.OrdinalIgnoreCase);
        private static ConcurrentDictionary<string, bool> existingClasses = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private static ManagementScope getScope(string wmic_namespace)
        {
            return scopes.GetOrAdd(wmic_namespace, ns =>
            {
                ManagementScope scope = new ManagementScope(ns);
                scope.Connect();
                return scope;
            });
        }

        private static bool classExists(ManagementScope scope, string wmic_namespace, string wmic_class)
        {
            return existingClasses.GetOrAdd(wmic_namespace + ":" + wmic_class, _ =>
            {
                SelectQuery classQuery = new SelectQuery("SELECT * FROM meta_class WHERE __class = '" + wmic_class + "'");
                using ManagementObjectSearcher classSearcher = new ManagementObjectSearcher(scope, classQuery);
                using ManagementObjectCollection classes = classSearcher.Get();
                return classes.Count > 0;
            });
        }

        public static string GetValue(string wmic_class, string wmic_selector, string wmic_namespace = @"root\wmi", int wmic_iterator_index = 0)
        {
            try
            {
                ManagementScope scope = getScope(wmic_namespace);

                if (!classExists(scope, wmic_namespace, wmic_class))
                {
                    App.log.writeLine("Wmic Class '" + wmic_class + "' not found in namespace " + wmic_namespace);
                    return "";
                }

                WqlObjectQuery query = new WqlObjectQuery("SELECT " + wmic_selector + " FROM " + wmic_class);
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query, null);
                using ManagementObjectCollection results = searcher.Get();

                int i = 0;
                foreach (ManagementBaseObject queryObj in results)
                {
                    using (queryObj)
                    {
                        if (wmic_iterator_index == i)
                        {
                            string? wmicValue = queryObj[wmic_selector]?.ToString();
                            App.log.writeLine("WMIC " + wmic_namespace + " SELECT " + wmic_selector + " FROM " + wmic_class + "[" + wmic_iterator_index + "] = " + wmicValue);
                            return wmicValue ?? "";
                        }
                    }

                    i++;
                }
            }
            catch (Exception e) when (e is ManagementException || e is System.Runtime.InteropServices.COMException || e is UnauthorizedAccessException)
            {
                App.log.writeLine("WMIC " + wmic_namespace + " SELECT " + wmic_selector + " FROM " + wmic_class + " ERROR: " + e.Message);
                // Drop the cached connection, it may be broken (e.g. after the WMI service restarted)
                scopes.TryRemove(wmic_namespace, out _);
            }

            return "";
        }
    }
}
