using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace HA.Class.Sensors
{
    class Wmic
    {
        public static string GetValue(string wmic_class, string wmic_selector, string wmic_namespace = @"root\\wmi")
        {
            Debug.WriteLine("NAMESPACE " + wmic_namespace);
            Debug.WriteLine("SELECT " + wmic_selector + " FROM " + wmic_class);

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmic_namespace, ("SELECT " + wmic_selector +  " FROM " + wmic_class));
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    if (!Equals(queryObj, null) && queryObj != null && queryObj != new ManagementObject() { })
                    {
                        if (queryObj.Properties.Count > 0 && queryObj?[wmic_selector]?.ToString() != null) { 
                            return queryObj?[wmic_selector]?.ToString();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("An error occurred while querying for WMI data: " + e.Message);
            }

            return "";
        }
    }
}
