using AutoUpdaterDotNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Helpers
{
    internal class AutoUpdateHelper
    {
        public AutoUpdateHelper()
        {
            AutoUpdater.ParseUpdateInfoEvent += AutoUpdaterOnParseUpdateInfoEvent;
            AutoUpdater.Synchronous = true;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.ClearAppDirectory = false;
            //AutoUpdater.ReportErrors = Debugger.IsAttached;
            AutoUpdater.HttpUserAgent = ("FakeDeck-v" + Assembly.GetExecutingAssembly().GetName().Version);
            AutoUpdater.Start("https://github.com/GamerClassN7/HA_Desktop_Companion/releases/latest/download/meta.xml");
        }

        private void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
        {
            JsonElement json = JsonDocument.Parse(args.RemoteData).RootElement;
            args.UpdateInfo = new UpdateInfoEventArgs
            {
                CurrentVersion = json.GetProperty("tag_name").ToString().TrimStart('v') + ".0",
                DownloadURL = json.GetProperty("zipball_url").ToString(),
            };
            Debug.WriteLine("calling Updater");
        }
    }
}
