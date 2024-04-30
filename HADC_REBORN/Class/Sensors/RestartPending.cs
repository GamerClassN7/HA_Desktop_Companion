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
            try
            {
                using (var rootKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending"))
                {
                    if (rootKey != null)
                    {
                        if (rootKey.GetSubKeyNames().Length > 0)
                            return true;
                    }
                }

                using (var rootKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
                {
                    if (rootKey != null)
                    {
                        if (rootKey.GetSubKeyNames().Length > 0)
                            return true;
                    }
                }

                using (var rootKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager"))
                {
                    if (rootKey != null)
                    {
                        if (rootKey.GetValueKind("PendingFileRenameOperations") != null)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception) 
            {
            }
            
            return false;
        }
    }
}
