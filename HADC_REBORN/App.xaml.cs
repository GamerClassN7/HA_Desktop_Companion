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

        public static NotifyIcon ?icon = null;
        public static Logger log = new Logger();
        public static YamlLoader ?yamlLoader = null;
        
        public static ApiConnector ?haApiConnector = null;
        public static ApiWrapper ?apiWrapper = null;

        public static WsConnector? haWsConnector = null;
        public static WsWrapper? wsWrapper = null;

        private static DispatcherTimer ?apiTimer = null;
        private BackgroundWorker apiWorker;
        public bool isInitialization = true;

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
            apiWorker = new BackgroundWorker();

            Start();

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
            } else {
                log.writeLine("'configuration.yaml' found!");
            }

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string url = config.AppSettings.Settings["url"].Value;
            string token = config.AppSettings.Settings["token"].Value;
            string webhookId = config.AppSettings.Settings["webhook_id"].Value;
            string secret = config.AppSettings.Settings["secret"].Value;

            yamlLoader = new YamlLoader(configFilePath);

            do
            {
                log.writeLine("Waiting ntil server response!");
            } while (!Network.PingHost((new Uri(url)).Host));

          try
            {
                log.writeLine(url);
                haApiConnector = new ApiConnector(url, token);
                apiWrapper = new ApiWrapper(yamlLoader, haApiConnector);
                
                if (String.IsNullOrEmpty(webhookId))
                {
                    ApiDevice devideForRegistration = new ApiDevice()
                    {
                        device_name = Environment.MachineName,
                        device_id = (Environment.MachineName).ToLower(),
                        app_id = Assembly.GetEntryAssembly().GetName().Version.ToString().ToLower(),
                        app_name = Assembly.GetExecutingAssembly().GetName().Name,
                        app_version = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                        manufacturer = Wmic.GetValue("Win32_ComputerSystem", "Manufacturer", "root\\CIMV2"),
                        model = Wmic.GetValue("Win32_ComputerSystem", "Model", "root\\CIMV2"),
                        os_name = Wmic.GetValue("Win32_OperatingSystem", "Caption", "root\\CIMV2"),
                        os_version = Environment.OSVersion.ToString(),
                        app_data = new {
                            push_websocket_channel = true,
                        },
                        supports_encryption = false
                    };
                    haApiConnector.RegisterDevice(devideForRegistration);

                    Dictionary<string, object> senzorTypes = apiWrapper.getSensorsConfiguration();
                    foreach (var item in senzorTypes)
                    {
                        string senzorType = item.Key;
                        foreach (var platform in (Dictionary<string, Dictionary<string, List<Dictionary<string, dynamic>>>>)senzorTypes[senzorType])
                        {
                            foreach (var integration in (Dictionary<string, List<Dictionary<string, dynamic>>>)platform.Value)
                            {
                                foreach (var sensorDefinition in (List<Dictionary<string, dynamic>>)integration.Value)
                                {
                                    ApiSensor senzor = new ApiSensor();

                                    senzor.type = senzorType;
                                    senzor.name = sensorDefinition["name"];
                                    senzor.unique_id = sensorDefinition["unique_id"];

                                    if (sensorDefinition.ContainsKey("device_class"))
                                        senzor.device_class = sensorDefinition["device_class"];

                                    if (sensorDefinition.ContainsKey("icon"))
                                        senzor.icon = sensorDefinition["icon"];

                                    if (sensorDefinition.ContainsKey("unit_of_measurement"))
                                        senzor.unit_of_measurement = sensorDefinition["unit_of_measurement"];

                                    if (sensorDefinition.ContainsKey("state_class"))
                                        senzor.state_class = sensorDefinition["state_class"];

                                    if (sensorDefinition.ContainsKey("entity_category"))
                                        senzor.entity_category = sensorDefinition["entity_category"];

                                    if (sensorDefinition.ContainsKey("disabled"))
                                        senzor.device_class = sensorDefinition["disabled"];

                                    if (senzorType == "binary_sensor")
                                        senzor.state = false;

                                    haApiConnector.RegisterSensorData(senzor);
                                }
                            }
                        }
                    }

                    webhookId = haApiConnector.getWebhookID();
                    secret = haApiConnector.getSecret();

                    config.AppSettings.Settings["webhook_id"].Value = webhookId;
                    config.AppSettings.Settings["secret"].Value = secret;

                    config.Save(ConfigurationSaveMode.Modified);
                }

                haApiConnector.setWebhookID(webhookId);
                haApiConnector.setSecret(secret);

                apiTimer = new DispatcherTimer();
                apiTimer.Interval = TimeSpan.FromSeconds(5);
                apiTimer.Tick += updateSensors;
                apiTimer.Start();

                apiWorker.DoWork += apiWorker_DoWork;
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

            return true;
        }

        public void Stop()
        {
            log.writeLine("stoping RestAPI");
            apiTimer.Stop();
        }

        public bool isRunning()
        {
            return haApiConnector.getConectionStatus();
        }

        private async void updateSensors(object? sender, EventArgs e)
        {
            if (apiWorker.IsBusy != true)
            {
                apiWorker.RunWorkerAsync();
            }
        }

        private void apiWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            apiWrapper.queryAndSendSenzorData();
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
