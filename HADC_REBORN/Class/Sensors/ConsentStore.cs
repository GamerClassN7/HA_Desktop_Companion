using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Sensors
{
    class ConsentStore
    {
        public static bool GetValue(string consent_category)
        {
            string[] consentStores = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + consent_category + @"\" ,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\" + consent_category + @"\NonPackaged"
            };

            try
            {
                foreach (string path in consentStores)
                {
                    using (var rootKey = Registry.CurrentUser.OpenSubKey(path))
                    {
                        if (rootKey == null)
                        {
                            continue;
                        }

                        foreach (var subKeyName in rootKey.GetSubKeyNames())
                        {
                            using (var subKey = rootKey.OpenSubKey(subKeyName))
                            {
                                if (!subKey.GetValueNames().Contains("LastUsedTimeStop"))
                                {
                                    continue;
                                }

                                var endTime = (long)subKey.GetValue("LastUsedTimeStop");
                                if (endTime == 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }

                    // Repeat the same search now under "LocalMachine"
                    using (var rootKey = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (rootKey == null)
                        {
                            continue;
                        }

                        foreach (var subKeyName in rootKey.GetSubKeyNames())
                        {
                            using (var subKey = rootKey.OpenSubKey(subKeyName))
                            {
                                if (!subKey.GetValueNames().Contains("LastUsedTimeStop"))
                                {
                                    continue;
                                }

                                var endTime = (long)subKey.GetValue("LastUsedTimeStop");
                                if (endTime == 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                App.log.writeLine("An error occurred while querying for CONSENT_STORE data: " + e.Message);
            }

            return false;
        }
    }
}
