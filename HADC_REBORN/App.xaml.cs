using HADC_REBORN.Class.Helpers;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;

using From = System.Windows.Forms;
using System.Diagnostics;
using System.Net.Mail;
using AutoUpdaterDotNET;
using System.Reflection.Metadata.Ecma335;
using System.IO;

namespace HADC_REBORN
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
#if DEBUG
        private string appDir = Directory.GetCurrentDirectory();
#else
        private string appDir = AppDomain.CurrentDomain.BaseDirectory();
#endif

        public static NotifyIcon icon;
        public static Logger log;

        protected override void OnStartup(StartupEventArgs e)
        {
            App.icon = new NotifyIcon();
            App.log = new Logger();

            icon.DoubleClick += new EventHandler(icon_Click);
            icon.Icon = HADC_REBORN.Resource.ha_icon;
            icon.Text = System.AppDomain.CurrentDomain.FriendlyName;
            icon.Visible = true;

            icon.ContextMenuStrip = new ContextMenuStrip();
            icon.ContextMenuStrip.Items.Add("Home Assistant", null, OnHomeAssistant_Click);
            icon.ContextMenuStrip.Items.Add("Log", null, OnLog_Click);
            icon.ContextMenuStrip.Items.Add("Send Test Notification", null, OnTestNotification_Click);
            icon.ContextMenuStrip.Items.Add("Quit", null, OnQuit_Click);

            base.OnStartup(e);
        }

        private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
        }


        private void Application_Exit(object sender, ExitEventArgs e)
        {

        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Start();

            AutoUpdater.Start("https://github.com/GamerClassN7/HA_Desktop_Companion/releases/latest/download/update_meta.xml");
            AutoUpdater.Synchronous = false;
            AutoUpdater.ShowRemindLaterButton = false;
        }

        private void Start()
        {
            log.writeLine("looking for 'configuration.yaml'");
            string configFilePath = Path.Combine(appDir, "configuration.yaml");
            if (!File.Exists(configFilePath))
            {
                log.writeLine("'configuration.yaml' not found creating new one!");
                File.WriteAllBytes(configFilePath, HADC_REBORN.Resource.configuration);
            } else {
                log.writeLine("'configuration.yaml' found!");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            icon.Dispose();

            base.OnExit(e);
        }

        private void OnLog_Click(object? sender, EventArgs e)
        {
            Process.Start("notepad", log.getLogPath());
        }

        private void OnHomeAssistant_Click(object? sender, EventArgs e)
        {
            Process.Start("explorer", "https://google.com");
        }

        private void OnQuit_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void OnTestNotification_Click(object? sender, EventArgs e)
        {
            SpawnNotification("test");
        }

        private void icon_Click(Object? sender, EventArgs e)
        {
            MainWindow main = App.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (main != null)
            {
                main.Focus();
            }
            else 
            {
                new MainWindow().Show();
            }
        }

        public static void Close()
        {
            Environment.Exit(0);
        }

        public static void SpawnNotification( string body = "", string title = "", string imageUrl = "", string audioUrl = "", int duration = 500)
        {
            ToastContentBuilder toast = new ToastContentBuilder();
            toast.AddText(body);

            if (!String.IsNullOrEmpty(title))
            {
                toast.AddText(title);
            }

            toast.Show();
        }

    }

}
