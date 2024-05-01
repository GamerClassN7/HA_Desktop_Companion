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
        public static ApiConnector haApiConnector;
        public static YamlLoader yamlLoader;

        private BackgroundWorker apiWorker;
        private static DispatcherTimer apiTimer;
        static Dictionary<string, DateTime> sensorUpdatedAtList = new Dictionary<string, DateTime>();
        static Dictionary<string, dynamic> sensorLastValues = new Dictionary<string, dynamic>();

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.FirstChanceException += GlobalExceptionFunction;

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

        public void Start()
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

            try
            {
                haApiConnector = new ApiConnector(url, token);
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

                    Dictionary<string, object> senzorTypes = getSensorsConfiguration();
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
                } else
                {
                    haApiConnector.setWebhookID(webhookId);
                    haApiConnector.setSecret(secret);
                }

                apiTimer = new DispatcherTimer();
                apiTimer.Interval = TimeSpan.FromSeconds(5);
                apiTimer.Tick += updateSensors;
                apiTimer.Start();

            }
            catch (Exception e)
            {
                log.writeLine("Failed to initialize RestAPI" + e.Message);
            }

            apiWorker.DoWork += apiWorker_DoWork;
        }

        public void Stop()
        {
            log.writeLine("stoping RestAPI");
            apiTimer.Stop();
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
            queryAndSendSenzorData();
        }

        private static Dictionary<string, object> getSensorsConfiguration()
        {
            Dictionary<string, object> senzorTypes = new Dictionary<string, object>();
            senzorTypes.Add("sensor", yamlLoader.getConfigurationData()["sensor"]);

            if (yamlLoader.getConfigurationData().ContainsKey("binary_sensor"))
                senzorTypes.Add("binary_sensor", yamlLoader.getConfigurationData()["binary_sensor"]);

            return senzorTypes;
        }

        private static async Task<String> getSenzorValue(KeyValuePair<string, List<Dictionary<string, dynamic>>> integration, Dictionary<string, dynamic> sensorDefinition)
        {
            string className = "HADC_REBORN.Class.Sensors.";

            foreach (var methodNameSegment in integration.Key.Split("_"))
            {
                className += methodNameSegment[0].ToString().ToUpper() + methodNameSegment.Substring(1);
            }

            Type SensorTypeClass = Type.GetType(className);
            if (SensorTypeClass == null)
            {
                App.log.writeLine(className + " Class Not Found");
                throw new Exception(className + " Class Not Found");
            }

            MethodInfo method = SensorTypeClass.GetMethod("GetValue");
            if (method == null)
            {
                App.log.writeLine("GetValue Method Not Found on " + className);
                throw new Exception("GetValue Method Not Found on " + className);
            }

            ParameterInfo[] pars = method.GetParameters();
            List<object> parameters = new List<object>();

            foreach (ParameterInfo p in pars)
            {
                if (sensorDefinition.ContainsKey(p.Name))
                {
                    parameters.Insert(p.Position, sensorDefinition[p.Name]);
                }
                else if (p.IsOptional)
                {
                    parameters.Insert(p.Position, p.DefaultValue);
                }
            }

            return method.Invoke(null, parameters.ToArray()).ToString();
        }

        private async Task queryAndSendSenzorData()
        {
            Dictionary<string, Task<string>> senzorsQuerys = new Dictionary<string, Task<string>>();

            Dictionary<string, object> senzorTypes = getSensorsConfiguration();
            foreach (var item in senzorTypes)
            {
                string senzorType = item.Key;
                foreach (var platform in (Dictionary<string, Dictionary<string, List<Dictionary<string, dynamic>>>>)senzorTypes[senzorType])
                {
                    foreach (var integration in (Dictionary<string, List<Dictionary<string, dynamic>>>)platform.Value)
                    {
                        foreach (var sensorDefinition in (List<Dictionary<string, dynamic>>)integration.Value)
                        {
                            string sensorUniqueId = sensorDefinition["unique_id"];
                            if (senzorsQuerys.ContainsKey(sensorUniqueId))
                            {
                                continue;
                            }

                            if (sensorUpdatedAtList.ContainsKey(sensorUniqueId) && sensorDefinition.ContainsKey("update_interval"))
                            {
                                TimeSpan difference = DateTime.Now.Subtract(sensorUpdatedAtList[sensorUniqueId]);
                                if (difference.TotalSeconds < Double.Parse(sensorDefinition["update_interval"]))
                                {
                                    continue;
                                }
                            }

                            senzorsQuerys.Add(sensorUniqueId, getSenzorValue(integration, sensorDefinition));
                        }
                    }
                }
            }

            //TODO, Create Sensor list to iterate ower when building request to server

            await Task.WhenAll(senzorsQuerys.Values.ToArray());
            if (senzorsQuerys.Count < 1)
            {
                App.log.writeLine("no senzor scheduled!");
            }
            App.log.writeLine("all task query Done!");

            foreach (var item in senzorTypes)
            {
                string senzorType = item.Key;
                foreach (var platform in (Dictionary<string, Dictionary<string, List<Dictionary<string, dynamic>>>>)senzorTypes[senzorType])
                {
                    foreach (var integration in (Dictionary<string, List<Dictionary<string, dynamic>>>)platform.Value)
                    {
                        foreach (var sensorDefinition in (List<Dictionary<string, dynamic>>)integration.Value)
                        {
                            string sensorUniqueId = sensorDefinition["unique_id"];
                            if (!senzorsQuerys.ContainsKey(sensorUniqueId))
                            {
                                continue;
                            }

                            string sensorData = senzorsQuerys[sensorUniqueId].Result;
                            sensorData = applySenzorValueFilters(senzorType, sensorDefinition, sensorData);
                            App.log.writeLine("Filtered Value " + sensorUniqueId + " - " + sensorData);

                            if (string.IsNullOrEmpty(sensorData))
                            {
                                App.log.writeLine("No Data Returned to sensor " + sensorUniqueId);
                                continue;
                            }

                            if (sensorLastValues.ContainsKey(sensorDefinition["unique_id"]))
                            {
                                if (sensorData == sensorLastValues[sensorDefinition["unique_id"]])
                                {
                                    //App.log.writeLine("Skiping! Same Data Already Send " + sensorData);
                                    continue;
                                }
                            }

                            ApiSensor senzor = new ApiSensor();

                            senzor.unique_id = sensorDefinition["unique_id"];
                            senzor.icon = sensorDefinition["icon"];
                            senzor.state = convertToType(sensorData);
                            senzor.type = senzorType;
                            senzor.unique_id = sensorDefinition["unique_id"];

                            haApiConnector.AddSensorData(senzor);

                            if (sensorUpdatedAtList.ContainsKey(sensorDefinition["unique_id"]))
                            {
                                sensorUpdatedAtList[sensorDefinition["unique_id"]] = DateTime.Now;
                            }
                            else
                            {
                                sensorUpdatedAtList.Add(sensorDefinition["unique_id"], DateTime.Now);
                            }

                            if (sensorLastValues.ContainsKey(sensorDefinition["unique_id"]))
                            {
                                sensorLastValues[sensorDefinition["unique_id"]] = sensorData;
                            }
                            else
                            {
                                sensorLastValues.Add(sensorDefinition["unique_id"], sensorData);
                            }
                        }
                    }
                }
            }

            haApiConnector.sendSensorBuffer();
        }

        private static string applySenzorValueFilters(string senzorType, Dictionary<string, dynamic> sensorDefinition, string sensorData)
        {
            if (senzorType == "binary_sensor")
            {
                return sensorData;
            }

            if (string.IsNullOrEmpty(sensorData))
            {
                sensorData = "0";
            }

            if (sensorDefinition.ContainsKey("value_map"))
            {
                string[] valueMap = sensorDefinition["value_map"].Split("|");
                sensorData = valueMap[(Int32.Parse((sensorData).ToString()))];
                //Logger.write(JsonConvert.SerializeObject(valueMap));
            }

            if (sensorDefinition.ContainsKey("filters"))
            {
                bool isNumeric = double.TryParse(sensorData, out _);
                Dictionary<string, string> filters = sensorDefinition["filters"];

                if (isNumeric)
                {
                    if (filters.ContainsKey("multiply"))
                    {
                        sensorData = (double.Parse(sensorData) * float.Parse(filters["multiply"], CultureInfo.InvariantCulture.NumberFormat)).ToString();
                    }

                    if (filters.ContainsKey("divide"))
                    {
                        sensorData = (double.Parse(sensorData) / float.Parse(filters["divide"], CultureInfo.InvariantCulture.NumberFormat)).ToString();
                    }

                    if (filters.ContainsKey("deduct"))
                    {
                        sensorData = (double.Parse(sensorData) - float.Parse(filters["deduct"], CultureInfo.InvariantCulture.NumberFormat)).ToString();
                    }

                    if (filters.ContainsKey("add"))
                    {
                        sensorData = (double.Parse(sensorData) + float.Parse(filters["add"], CultureInfo.InvariantCulture.NumberFormat)).ToString();
                    }
                }

            }

            if (sensorDefinition.ContainsKey("accuracy_decimals"))
            {
                if (Regex.IsMatch(sensorData.ToString(), @"^[0-9]+.[0-9]+$") || Regex.IsMatch(sensorData.ToString(), @"^\d$"))
                {
                    sensorData = Math.Round(double.Parse(sensorData), Int32.Parse(sensorDefinition["accuracy_decimals"] ?? 0)).ToString();
                }
            }

            return sensorData;
        }

        private static dynamic convertToType(dynamic variable)
        {
            //ADD double 
            string variableStr = variable.ToString();
            // Logger.write("BEFORE CONVERSION" + variableStr);
            if (Regex.IsMatch(variableStr, "^(?:tru|fals)e$", RegexOptions.IgnoreCase))
            {
                //Logger.write("AFTER CONVERSION (Bool)" + variableStr.ToString());
                return bool.Parse(variableStr);
            }
            else if (Regex.IsMatch(variableStr, @"^[0-9]+.[0-9]+$") && (variableStr.Contains(".") || variableStr.Contains(",")))
            {
                //Logger.write("AFTER CONVERSION (double)" + variableStr.ToString());
                return double.Parse(variableStr);
            }
            else if (Regex.IsMatch(variableStr, @"^\d+$"))
            {
                //Logger.write("AFTER CONVERSION (int)" + variableStr.ToString());
                return double.Parse(variableStr);
            }

            //Logger.write("AFTER CONVERSION" + variableStr.ToString());
            return variableStr;
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
