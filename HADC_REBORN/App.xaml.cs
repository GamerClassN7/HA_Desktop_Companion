using HADC_REBORN.Class.Config;
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
using System.Net.Sockets;
using Windows.UI.ViewManagement;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Media.Imaging;

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
        private string appDir = AppDomain.CurrentDomain.BaseDirectory;
#endif

        public static NotifyIcon? icon = null;
        public static Logger log = new Logger();
        public static YamlLoader? yamlLoader;
        public bool initializing = true;

        public ApiConnector? haApiConnector = null;
        public static ApiWrapper? apiWrapper = null;

        public WsConnector? haWsConnector = null;
        public static WsWrapper? wsWrapper = null;

        public static string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        protected override void OnStartup(StartupEventArgs e)
        {

            foreach (string f in Directory.EnumerateFiles(appDir, "HA.*"))
            {
                File.Delete(f);
            }

            foreach (string f in Directory.EnumerateFiles(appDir, "ha.*"))
            {
                File.Delete(f);
            }

            foreach (string f in Directory.EnumerateFiles(appDir, "*.log"))
            {
                File.Delete(f);
            }

            foreach (string f in Directory.EnumerateFiles(appDir, "*.xml"))
            {
                File.Delete(f);
            }

            ensureAppSettings();

            // Log only exceptions nobody handled, FirstChanceException also logged every caught one
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
         
            App.icon = new NotifyIcon();

            icon.DoubleClick += new EventHandler(icon_Click);
            icon.Icon = HADC_REBORN.Resource.ha_icon;

            //Count for icon dakt mode change
            Theme.setTheme(Theme.isLightTheme());
           
            icon.Text = System.AppDomain.CurrentDomain.FriendlyName;
            icon.Visible = true;

            icon.ContextMenuStrip = new ContextMenuStrip();
            icon.ContextMenuStrip.Items.Add("Home Assistant", null, OnHomeAssistant_Click);
            icon.ContextMenuStrip.Items.Add("Log", null, OnLog_Click);
            icon.ContextMenuStrip.Items.Add("Send Test Notification", null, OnTestNotification_Click);
            icon.ContextMenuStrip.Items.Add("Quit", null, OnQuit_Click);

            base.OnStartup(e);

            //On Color mode 
            UISettings settings = new UISettings();
            Theme.setTheme(Theme.isColorLight(settings.GetColorValue(UIColorType.Background)));
            settings.ColorValuesChanged += theme_Changed;

            log.writeLine("starting version: " + version);
        }

        // Release zips don't contain HADC_REBORN.dll.config so updates keep the user's settings,
        // create missing keys (e.g. on a fresh install) so the rest of the app can rely on them
        private static void ensureAppSettings()
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                bool changed = false;
                foreach (string key in new string[] { "url", "token", "webhook_id", "remote_url", "cloud_url", "secret" })
                {
                    if (config.AppSettings.Settings[key] == null)
                    {
                        config.AppSettings.Settings.Add(key, "");
                        changed = true;
                    }
                }

                if (changed)
                {
                    config.Save(ConfigurationSaveMode.Modified);
                }
            }
            catch (Exception ex)
            {
                log.writeLine("Failed to initialize app settings: " + ex.Message);
            }
        }

        public void loadYAMLComfig(bool force = false)
        {
            if (yamlLoader != null && !force)
            {
                return;
            }

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

            try
            {
                yamlLoader = new YamlLoader(configFilePath);
                foreach (string warning in yamlLoader.Warnings)
                {
                    log.writeLine("[CONFIG] " + warning);
                }
                foreach (string error in yamlLoader.Errors)
                {
                    log.writeLine("[CONFIG] Sensor skipped: " + error, 3);
                }
                if (yamlLoader.Errors.Count > 0)
                {
                    notifyConfigProblem(String.Join("\n", yamlLoader.Errors), "Some sensors in configuration.yaml were skipped");
                }
            }
            catch (YamlConfigurationException ex)
            {
                // The app can't run without valid sensor definitions, tell the user where the problem is
                yamlLoader = null;
                log.writeLine("[CONFIG] " + ex.Message, 3);
                notifyConfigProblem(ex.Message, "Invalid configuration.yaml");
            }
        }

        private string lastConfigProblem = "";

        // The configuration is loaded more than once during startup, show each problem only once
        private void notifyConfigProblem(string message, string title)
        {
            if (message == lastConfigProblem)
            {
                return;
            }

            lastConfigProblem = message;
            Notification.Spawn(message, title);
        }

        public AppConfiguration? getYAMLComfig()
        {
            return yamlLoader?.getConfigurationData();
        }

        private void theme_Changed(UISettings sender, object args)
        {
            Theme.setTheme(Theme.isColorLight(sender.GetColorValue(UIColorType.Background)));
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            log.writeLine("[" + AppDomain.CurrentDomain.FriendlyName + "] Unhandled UI exception: " + e.Exception.ToString(), 3);
            // Keep the tray app running instead of crashing
            e.Handled = true;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            log.writeLine("[" + AppDomain.CurrentDomain.FriendlyName + "] Unhandled exception: " + e.ExceptionObject.ToString(), 3);
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            log.writeLine("[" + AppDomain.CurrentDomain.FriendlyName + "] Unobserved task exception: " + e.Exception.ToString(), 3);
            e.SetObserved();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {

        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                AutoUpdateHelper updater = new AutoUpdateHelper();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private bool starting = false;

        // Network heavy parts run in the background so the UI doesn't freeze while HA is unreachable.
        // Must be called from the UI thread, the wrappers create their DispatcherTimers in their constructors.
        public async Task<bool> StartAsync()
        {
            if (starting)
            {
                log.writeLine("Start already in progress");
                return false;
            }

            starting = true;
            try
            {
                return await startInternal();
            }
            finally
            {
                starting = false;
            }
        }

        private async Task<bool> startInternal()
        {
            loadYAMLComfig(true);
            if (yamlLoader == null)
            {
                return false;
            }

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string url = config.AppSettings.Settings["url"].Value;
            string token = config.AppSettings.Settings["token"].Value;
            log.setSecreets(new string[] { token, url.Replace("http://", "").Replace("https://", "")});


            if (String.IsNullOrEmpty(url) || String.IsNullOrEmpty(token))
            {
                log.writeLine("URL or Token not fount!!");
                return false;
            }

            try
            {
                log.writeLine(url);
                haApiConnector = new ApiConnector(url, token);
                apiWrapper = new ApiWrapper(yamlLoader.getConfigurationData(), haApiConnector, config);
                apiWrapper.WebhookChanged += apiWrapper_WebhookChanged;
                await Task.Run(() => apiWrapper.connect());
                log.writeLine("RestAPI registered");
                log.setSecreets(new string[] { token, url.Replace("http://", "").Replace("https://", ""), haApiConnector.getSecret(), haApiConnector.getWebhookID() });
            }
            catch (Exception ex)
            {
                log.writeLine("Failed to initialize RestAPI" + ex.Message);
                return false;
            }

            if (String.IsNullOrEmpty(haApiConnector.getWebhookID()))
            {
                log.writeLine("Failed to get webhook_id from RestAPI");
                return false;
            }

            try
            {
                string wsUrl = url.Replace("http", "ws");
                log.writeLine(wsUrl);
                haWsConnector = new WsConnector(wsUrl, token, haApiConnector.getWebhookID());
                wsWrapper = new WsWrapper(haWsConnector);
                await Task.Run(() => wsWrapper.Connect());
                log.writeLine("Websocket registered");
            }
            catch (Exception ex)
            {
                log.writeLine("Failed to initialize WebbSocket" + ex.Message);
                return false;
            }

            NetworkChange.NetworkAvailabilityChanged -= GetNetworkChange_NetworkAvailabilityChanged;
            NetworkChange.NetworkAvailabilityChanged += GetNetworkChange_NetworkAvailabilityChanged;

            try
            {
                Autostart.register();
                log.writeLine("Autostart registered");
            }
            catch (Exception ex)
            {
                log.writeLine("Autostart registration failed" + ex.Message);
                return false;
            }

            log.writeLine("Initialization Compleeted");
            return true;
        }

        // The device was registered again in HA (it had been deleted), the websocket must subscribe with the new webhook
        private void apiWrapper_WebhookChanged(string webhookId)
        {
            log.writeLine("Webhook changed, reconnecting WebSocket");

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string token = config.AppSettings.Settings["token"].Value;
            string url = config.AppSettings.Settings["url"].Value;
            log.setSecreets(new string[] { token, url.Replace("http://", "").Replace("https://", ""), haApiConnector?.getSecret() ?? "", webhookId });

            if (haWsConnector == null || wsWrapper == null)
            {
                return;
            }

            haWsConnector.setWebhookID(webhookId);
            try
            {
                wsWrapper.restart();
            }
            catch (Exception ex)
            {
                log.writeLine("Failed to reconnect WebSocket: " + ex.Message);
            }
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
                wsWrapper.Disconnect();
                apiWrapper.disconnect();
            }
        }

        public void Stop()
        {
            NetworkChange.NetworkAvailabilityChanged -= GetNetworkChange_NetworkAvailabilityChanged;

            log.writeLine("stoping RestAPI");
            if (apiWrapper != null)
            {
                apiWrapper.WebhookChanged -= apiWrapper_WebhookChanged;
                apiWrapper.disconnect();
            }

            log.writeLine("stoping WebSocket");
            wsWrapper?.Stop();

            // A re-registration during the next start must not touch the old connection
            wsWrapper = null;
            haWsConnector = null;
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
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string url = config.AppSettings.Settings["url"].Value;
            if (String.IsNullOrEmpty(url))
            {
                log.writeLine("Home Assistant URL not configured!");
                return;
            }

            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
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
