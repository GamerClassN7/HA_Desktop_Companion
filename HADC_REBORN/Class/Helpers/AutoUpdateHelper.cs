using AutoUpdaterDotNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Helpers
{
    internal class AutoUpdateHelper
    {
        public AutoUpdateHelper()
        {
            // meta.xml is in the AutoUpdater.NET XML format, it parses it itself (no ParseUpdateInfoEvent needed)
            AutoUpdater.Synchronous = true;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.ClearAppDirectory = false;
            //AutoUpdater.ReportErrors = Debugger.IsAttached;
            AutoUpdater.HttpUserAgent = ("HADC-v" + Assembly.GetExecutingAssembly().GetName().Version);
            AutoUpdater.Start("https://github.com/GamerClassN7/HA_Desktop_Companion/releases/latest/download/meta.xml");
        }
    }
}
