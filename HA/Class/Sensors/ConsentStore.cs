using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace HA.Class.Sensors
{
    class ConsentStore
    {
        public static bool GetValue(string consent_category)
        {
            try
            {
                string[] consentStores = {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + consent_category + @"\" ,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + consent_category + @"\NonPackaged"
                };

                foreach (string path in consentStores)
                {
                    using (var rootKey = Registry.CurrentUser.OpenSubKey(path))
                    {
                        if (rootKey != null)
                        {
                            foreach (var subKeyName in rootKey.GetSubKeyNames())
                            {
                                using (var subKey = rootKey.OpenSubKey(subKeyName))
                                {
                                    if (subKey.GetValueNames().Contains("LastUsedTimeStop"))
                                    {

                                        var endTime = (long)subKey.GetValue("LastUsedTimeStop");
                                        //Debug.WriteLine(consent_category + " " + subKey.GetValue("LastUsedTimeStop"));

                                        if (endTime == 0)
                                        {
                                            //MessageBox.Show(subKey.GetValue("LastUsedTimeStop").ToString());
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    // Repeat the same search now under "LocalMachine"
                    using (var rootKey = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (rootKey != null)
                        {
                            foreach (var subKeyName in rootKey.GetSubKeyNames())
                            {

                                using (var subKey = rootKey.OpenSubKey(subKeyName))
                                {
                                    if (subKey.GetValueNames().Contains("LastUsedTimeStop"))
                                    {

                                        var endTime = (long)subKey.GetValue("LastUsedTimeStop");
                                        //Debug.WriteLine(consent_category + " " + subKey.GetValue("LastUsedTimeStop"));

                                        if (endTime == 0)
                                        {
                                            //MessageBox.Show(subKey.GetValue("LastUsedTimeStop").ToString());
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            return false;
        }
    }
}
