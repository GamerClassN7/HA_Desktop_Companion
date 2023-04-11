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
            try
            {
                Debug.WriteLine("NAMESPACE " + wmic_namespace);
                Debug.WriteLine("SELECT " + wmic_selector + " FROM " + wmic_class);
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmic_namespace, ("SELECT " + wmic_selector +  " FROM " + wmic_class));
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        if (queryObj != null && queryObj != new ManagementObject() { })
                        {
                            return queryObj[wmic_selector].ToString();
                        }
                    }
                }
                catch (ManagementException e)
                {
                    MessageBox.Show("An error occurred while querying for WMI data: " + e.Message);
                }

                return "";
            }
            catch (Exception)
            {

                return "";
            }
        }
    }
}
