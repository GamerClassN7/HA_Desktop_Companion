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
using System.ComponentModel;
using System.Security.Policy;
using HADC_REBORN.Class.HomeAssistant;
using HADC_REBORN.Class.HomeAssistant.Objects;
using HADC_REBORN.Class.Sensors;
using System.Reflection;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Globalization;
using Windows.Devices.Sensors;
using System.Runtime.ExceptionServices;
using HADC_REBORN.Class.Actions;
using System.Net.NetworkInformation;

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

        public static NotifyIcon? icon = null;
        public static Logger log = new Logger();
        public static YamlLoader yamlLoader;
        public bool initializing = true;

        public static ApiConnector? haApiConnector = null;
        public static ApiWrapper? apiWrapper = null;

        public static WsConnector? haWsConnector = null;
        public static WsWrapper? wsWrapper = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.FirstChanceException += GlobalExceptionFunction;

            App.icon = new NotifyIcon();

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

        static void GlobalExceptionFunction(object source, FirstChanceExceptionEventArgs eventArgs)
        {
            log.writeLine("[" + AppDomain.CurrentDomain.FriendlyName + "]" + eventArgs.Exception.ToString(), 3);
        }

        private void Application_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {

        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AutoUpdater.Start("https://github.com/GamerClassN7/HA_Desktop_Companion/releases/latest/download/update_meta.xml");
            AutoUpdater.Synchronous = false;
            AutoUpdater.ShowRemindLaterButton = false;
        }

        public bool Start()
        {
            log.writeLine("looking for 'configuration.yaml'");
            string configFilePath = Path.Combine(appDir, "configuration.yaml");
            if (!File.Exists(configFilePath))
            {
                log.writeLine("'configuration.yaml' not found creating new one!");
                File.WriteAllBytes(configFilePath, HADC_REBORN.Resource.configuration);
            }
            else
            {
                log.writeLine("'configuration.yaml' found!");
            }

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string url = config.AppSettings.Settings["url"].Value;
            string token = config.AppSettings.Settings["token"].Value;

            yamlLoader = new YamlLoader(configFilePath);

            if (String.IsNullOrEmpty(url) || String.IsNullOrEmpty(token))
            {
                log.writeLine("URL or Token not fount!!");
                return false;
            }

            do
            {
                log.writeLine("Waiting ntil server response!");
            } while (!Network.PingHost((new Uri(url)).Host));

            try
            {
                log.writeLine(url);
                haApiConnector = new ApiConnector(url, token);
                apiWrapper = new ApiWrapper(yamlLoader, haApiConnector, config);
                apiWrapper.connect();
            }
            catch (Exception e)
            {
                log.writeLine("Failed to initialize RestAPI" + e.Message);
                return false;
            }

            try
            {
                string wsUrl = url.Replace("http", "ws");
                log.writeLine(wsUrl);
                haWsConnector = new WsConnector(wsUrl, token, haApiConnector.getWebhookID());
                wsWrapper = new WsWrapper(yamlLoader, haWsConnector);
                wsWrapper.Connect();
            }
            catch (Exception e)
            {
                log.writeLine("Failed to initialize WebbSocket" + e.Message);
                return false;
            }

            NetworkChange.NetworkAvailabilityChanged += GetNetworkChange_NetworkAvailabilityChanged;

            return true;
        }

        private void GetNetworkChange_NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            if (e.IsAvailable)
            {
                log.writeLine("Network Connected!");
                wsWrapper.restart();
                apiWrapper.restart();
            }
            else
            {
                log.writeLine("Network Disconnected!");
            }
        }

        public void Stop()
        {
            log.writeLine("stoping RestAPI");
        }

        public bool isRunning()
        {
            return (haApiConnector.connected() && haWsConnector.connected());
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
            Notification.Spawn("test");
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
    }
}
