using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Sensors
{
    internal class RestartPending
    {
        public static bool GetValue()
        {
            string[] registerStores = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending" ,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"
            };

            try
            {

                foreach (string path in registerStores)
                {
                    using (var rootKey = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (rootKey != null)
                        {
                            if (rootKey.GetSubKeyNames().Length > 0)
                                return true;
                        }
                    }
                }

                using (var rootKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager"))
                {
                    if (rootKey != null)
                    {
                        if (!String.IsNullOrEmpty((string) rootKey.GetValue("PendingFileRenameOperations").ToString()))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception e) 
            {
                App.log.writeLine("An error occurred while querying for RESTART data: " + e.Message);
            }

            return false;
        }
    }
}
