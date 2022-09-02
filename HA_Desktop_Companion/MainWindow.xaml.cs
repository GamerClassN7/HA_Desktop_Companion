using System.Windows;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Reflection;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using HA_Desktop_Companion.Libraries;
using System.Net;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Logging;
using Microsoft.VisualBasic;
using System.Windows.Media.Media3D;
using Windows.Networking;
using Windows.AI.MachineLearning;
using System.Timers;
using System.Threading;
using Form = System.Windows.Forms;
using Application = System.Windows.Application;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace HA_Desktop_Companion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string devicePostFix = "_DEBUG";
        private static string appDir = Directory.GetCurrentDirectory();

        Logging log = new Logging(appDir + "/log.txt");
        Configuration config = new Configuration(appDir + "/configuration.yaml");
        HAApi_v2 apiConector;
        HAApi_Websocket wsConector;

        private bool isRegistered = false;
        private Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>> configData;
        private DispatcherTimer syncer = new DispatcherTimer();
        private int syncerIterator = 0;

        public MainWindow()
        {
            InitializeComponent();

            //Unaciunted Exceoption Handle
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(AllUnhandledExceptions);
            SystemEvents.PowerModeChanged += new PowerModeChangedEventHandler(OnPowerModeChanged);
        }

        private void AllUnhandledExceptions(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            MessageBox.Show(ex.ToString());
            log.Write(ex.ToString());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            log.Write("MAIN ->LOADED Start");
            //registration.IsEnabled = false;

            //Load Settings
            string decodedApiToken = Encryption.ToInsecureString(Encryption.DecryptString(Properties.Settings.Default.apiToken));
            string decodedWebhookId = Encryption.ToInsecureString(Encryption.DecryptString(Properties.Settings.Default.apiWebhookId));
            string base_url = Properties.Settings.Default.apiBaseUrl;
            string remote_ui_url = Properties.Settings.Default.apiRemoteUiUrl;
            string cloudhook_url = Properties.Settings.Default.apiCloudhookUrl;
            log.Write("MAIN ->Setting Loaded");


            //Set UI data
            apiToken.Password = decodedApiToken;
            apiBaseUrl.Text = base_url;
            version.Content = Assembly.GetEntryAssembly().GetName().Version.ToString();
            log.Write("MAIN ->UI Set");


            //Load Config
            if (!config.load())
            {
                log.Write("MAIN-failedLoad-configuration.yaml");
                MessageBox.Show("Config Error Report to Developer!");
            }
            configData = config.GetConfigurationData();
            log.Write("MAIN-configuration.yaml LOADED");

            //Prepare Syncer
            if (!RegisterSyncer())
            {
                log.Write("MAIN-Failed to register Sync Timer");
                MessageBox.Show("Syncer Error Report to Developer!");
            }
            log.Write("MAIN-Sync Timer Registered");

            //Check if already registered
            if (!String.IsNullOrEmpty(decodedWebhookId))
            {
                log.Write("MAIN-Previous Run detected trying to autostart");

                apiConector = new HAApi_v2(base_url, decodedApiToken, log, decodedWebhookId, remote_ui_url, cloudhook_url);
                apiConector.enableDebug(true);
                if (!apiConector.validateHaUrl())
                {
                    log.Write("MAIN-URL-STATUS:Not Valid");
                    registration.IsEnabled = true;
                } else {
                    log.Write("MAIN-URL-STATUS:Validated");

                    //Get Info for web hook id validation
                    string hostname = "";
                    string maufactorer = "";
                    string model = "";
                    string os = "";
                    string osVersion = "";

                    try
                    {
                        hostname = Dns.GetHostName() + devicePostFix;
                        maufactorer = Sensors.queryWmic("Win32_ComputerSystem", "Manufacturer", @"\\root\CIMV2");
                        model = Sensors.queryWmic("Win32_ComputerSystem", "Model", @"\\root\CIMV2");
                        osVersion = Environment.OSVersion.ToString();
                    }
                    catch (Exception)
                    {
                        log.Write("MAIN-Failed to Fetch Data for Webhook ID Validation !!!");
                        registration.IsEnabled = true;
                    }

                    //Validate Webhook ID
                    if (apiConector.validateWebhookId(hostname, model, maufactorer, osVersion))
                    {
                        log.Write("MAIN-Webhook ID valid!");

                        try
                        {
                            startSyncer();
                            log.Write("MAIN-Autostart Sucesfull!");

                            //Connect WS
                            wsConector = new HAApi_Websocket(apiBaseUrl.Text, apiToken.Password, log, apiConector.api_webhook_id, apiConector.api_remote_ui_url, apiConector.api_cloudhook_url);
                            log.Write("MAIN-WS Registered!");

                        }
                        catch (Exception)
                        {
                            log.Write("MAIN-Autostart Failed!");
                            registration.IsEnabled = true;
                        }
                    } else
                    {
                        log.Write("MAIN-Autostart Failed!");
                        log.Write("MAIN-Webhook ID invalid!");
                        registration.IsEnabled = true;
                    }
                }
            }

            log.Write("MAIN ->LOADED Done");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            log.Write("MAIN ->CLOSING TO TRAY");

            var app = Application.Current as App;
            app.ShowNotification(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, "App keeps Running in background!");

            this.ShowInTaskbar = false;
            this.Hide();

            e.Cancel = true;
        }

        private void debug_Checked(object sender, RoutedEventArgs e)
        {

        }

        private async void registrationAsync_Click(object sender, RoutedEventArgs e)
        {
            registration.IsEnabled = false;
            log.Write("MAIN-Registration Clicked");

            //check if inputs are not null
            if (String.IsNullOrEmpty(apiToken.Password) || String.IsNullOrEmpty(apiBaseUrl.Text))
            {
                log.Write("MAIN-Token/URL=Null");
                registration.IsEnabled = true;
                return;
            }

            //Validate Server URL
            log.Write("MAIN-URL:" + apiBaseUrl.Text);
            apiConector = new HAApi_v2(apiBaseUrl.Text, apiToken.Password, log);
            apiConector.enableDebug(true);
            if (!apiConector.validateHaUrl())
            {
                log.Write("MAIN-URL-STATUS:Not Valid");
                registration.IsEnabled = true;
                return;
            }
            log.Write("MAIN-URL-STATUS:Validated");


            //Get Info for Device Registration
            string hostname = "";
            string maufactorer = "";
            string model = "";
            string os = "";
            string osVersion = "";

            try
            {
                hostname = Dns.GetHostName() + devicePostFix;
                maufactorer = Sensors.queryWmic("Win32_ComputerSystem", "Manufacturer", @"\\root\CIMV2");
                model = Sensors.queryWmic("Win32_ComputerSystem", "Model", @"\\root\CIMV2");
                os = Sensors.queryWmic("Win32_OperatingSystem", "Caption", @"\\root\CIMV2");
                osVersion = Environment.OSVersion.ToString();
            }
            catch (Exception)
            {
                log.Write("MAIN-Failed to Fetch Data for Registration!!!");
                registration.IsEnabled = true;
                return;
            }

            //Register Device
            if (!apiConector.registerHaDevice(hostname.ToLower(), hostname, model, maufactorer, os, osVersion))
            {
                log.Write("MAIN-Device Failed to Register in API");
                registration.IsEnabled = true;
                return;
            }
            log.Write("MAIN-Device Registere in API");

            //Save Settings
            Properties.Settings.Default.apiBaseUrl = apiBaseUrl.Text;
            Properties.Settings.Default.apiToken = Encryption.EncryptString(Encryption.ToSecureString(apiToken.Password));
            Properties.Settings.Default.apiWebhookId = Encryption.EncryptString(Encryption.ToSecureString(apiConector.api_webhook_id));
            Properties.Settings.Default.apiRemoteUiUrl = apiConector.api_remote_ui_url;
            Properties.Settings.Default.apiCloudhookUrl = apiConector.api_cloudhook_url;
            Properties.Settings.Default.Save();
            log.Write("MAIN-Valid Settings Saved");


            //Entity Registration
            await RegisterApiData(apiConector);

            //autostart to REG
            if (!WriteAutostartToReg())
            {
                log.Write("MAIN-Failed to autoregister app in REG");
                registration.IsEnabled = true;
                return;
            }
            log.Write("MAIN-App registered in REG");

            //Start syncer
            startSyncer();

            //Connect WS
            wsConector = new HAApi_Websocket(apiBaseUrl.Text, apiToken.Password, log, apiConector.api_webhook_id, apiConector.api_remote_ui_url, apiConector.api_cloudhook_url);
            log.Write("MAIN-WS Registered!");

            registrationText.Text= "Registered";
        }

        private void quit_Click(object sender, RoutedEventArgs e)
        {
            SystemEvents.PowerModeChanged -= new PowerModeChangedEventHandler(OnPowerModeChanged);
            log.Write("MAIN -> Exit");
            Environment.Exit(0);
        }

        private async Task RegisterApiData(HAApi_v2 ApiConnection)
        {
            log.Write("MAIN -> Entity Registration");

            //Register Senzors
            Dictionary<string, object> senzorTypes = new Dictionary<string, object>();
            senzorTypes.Add("sensor", configData["sensor"]);

            if (configData.ContainsKey("binary_sensor"))
                senzorTypes.Add("binary_sensor", configData["binary_sensor"]);

            foreach (var item in senzorTypes)
            {
                string senzorType = item.Key;
                Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>> platforms = (Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>)senzorTypes[senzorType];
                foreach (var platform in platforms)
                {
                    Dictionary<string, List<Dictionary<string, string>>> integrations = (Dictionary<string, List<Dictionary<string, string>>>)platform.Value;
                    foreach (var integration in integrations)
                    {
                        List<Dictionary<string, string>> senzors = (List<Dictionary<string, string>>)integration.Value;
                        foreach (var senzor in senzors)
                        {

                            string deviceClass = "";
                            if (senzor.ContainsKey("device_class"))
                                deviceClass = senzor["device_class"];

                            if (Int32.Parse(Sensors.queryWmic("Win32_ComputerSystem", "PCSystemType", @"\\root\CIMV2")) != 2 && deviceClass == "battery")
                                continue;

                            string entityCategory = "";
                            if (senzor.ContainsKey("entity_category"))
                                entityCategory = senzor["entity_category"];

                            string icon = "";
                            if (senzor.ContainsKey("icon"))
                                icon = senzor["icon"];

                            string unitOfMeasurement = "";
                            if (senzor.ContainsKey("unit_of_measurement"))
                                unitOfMeasurement = senzor["unit_of_measurement"];

                            object defaultValue = "";
                            if (senzorType == "binary_sensor")
                                defaultValue = false;

                            await Task.Run(() => ApiConnection.registerHaEntiti(senzor["unique_id"], senzor["name"], defaultValue, senzorType, deviceClass, entityCategory, icon, unitOfMeasurement));
                        }
                    }
                }
            }
        }

        private bool WriteAutostartToReg()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string exePath = Path.Combine(assemblyFolder, "HA_Desktop_Companion.exe");
                    key.SetValue("HA_Desktop_Companion", "\"" + exePath + "\"");
                    key.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        private bool RegisterSyncer()
        {
            try
            {
                syncer.Tick += new EventHandler(syncerTickAsync);
                syncer.Interval = new TimeSpan(0, 0, 10);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        private void startSyncer()
        {
            syncer.Start();
        }

        private async void syncerTickAsync(object sender, EventArgs e)
        {
            syncer.Stop();
            try
            {
                log.Write("MAIN/SYNCER/TICK/" + syncerIterator);

                await sendApiDataParallelAsync(apiConector);
                wsConector.Check();
            }
            catch (Exception ex)
            {
                log.Write("MAIN/SYNCER/TICK/ERROR/" + ex.Message);
            }

            syncerIterator++;

            syncer.Start();
            log.housekeeping();
        }

        private async Task sendApiDataParallelAsync(HAApi_v2 ApiConnection)
        {
            var sensorsClass = typeof(Sensors);
            Dictionary<string, object> senzorTypes = new Dictionary<string, object>();
            senzorTypes.Add("sensor", configData["sensor"]);
            List<Task<bool>> tasks = new List<Task<bool>>();

            //Add Binary Senzors to Dictionary
            if (configData.ContainsKey("binary_sensor"))
                senzorTypes.Add("binary_sensor", configData["binary_sensor"]);

            //Parse each Senzor and aquire data
            foreach (var item in senzorTypes)
            {
                string senzorType = item.Key;
                Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>> platforms = (Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>)senzorTypes[senzorType];
                foreach (var platform in platforms)
                {
                    Dictionary<string, List<Dictionary<string, string>>> integrations = (Dictionary<string, List<Dictionary<string, string>>>)platform.Value;
                    foreach (var integration in integrations)
                    {
                        List<Dictionary<string, string>> sensors = (List<Dictionary<string, string>>)integration.Value;
                        foreach (var sensor in sensors)
                        {
                            tasks.Add(Task.Run(() => getEntityData(ApiConnection, integration.Key, sensorsClass, sensor, senzorType)));
                        }
                    }
                }
            }

            //Finish all tasks
            var results = await Task.WhenAll(tasks);
            foreach (var item in results)
            {
                log.Write("SENSOR/READ/" + item.ToString());
            }

            //Send Data
            ApiConnection.sendHaEntitiData();
        }

        private bool getEntityData(HAApi_v2 ApiConnection, string integration, Type sensorsClass, Dictionary<string, string> sensor, string sensorType)
        {
            try
            {
                object sensorData = null;
                string methodName = "query";

                foreach (var methodNameSegment in integration.Split("_"))
                {
                    methodName += methodNameSegment[0].ToString().ToUpper() + methodNameSegment.Substring(1);
                }

                MethodInfo method = sensorsClass.GetMethod(methodName);
                if (method == null)
                    return false;

                ParameterInfo[] pars = method.GetParameters();
                List<object> parameters = new List<object>();

                foreach (ParameterInfo p in pars)
                {
                    if (sensor.ContainsKey(p.Name))
                    {
                        parameters.Insert(p.Position, sensor[p.Name]);
                    }
                    else if (p.IsOptional)
                    {
                        parameters.Insert(p.Position, p.DefaultValue);
                    }
                }

                sensorData = method.Invoke(this, parameters.ToArray());

                if (sensorData != null)
                {
                    if (sensor.ContainsKey("value_map"))
                    {
                        string[] valueMap = sensor["value_map"].Split("|");
                        sensorData = valueMap[(Int32.Parse((sensorData).ToString()))];
                    }

                    if (sensor.ContainsKey("accuracy_decimals"))
                    {
                        try
                        {
                            if (Regex.IsMatch(sensorData.ToString(), @"^[0-9]+.[0-9]+$") || Regex.IsMatch(sensorData.ToString(), @"^\d$")) { 
                                sensorData = Math.Round(double.Parse(sensorData.ToString()), Int32.Parse(sensor["accuracy_decimals"]));
                            }
                        }
                        catch (Exception)
                        {

                        
                        }
                    }

                    sensorData = Sensors.convertToType(sensorData);

                    if (sensorType == "binary_sensor")
                    {
                        ApiConnection.addHaEntitiData(sensor["unique_id"], sensorData, "binary_sensor", sensor["icon"]);
                    }
                    else
                    {
                        ApiConnection.addHaEntitiData(sensor["unique_id"], sensorData, "sensor", sensor["icon"]);
                    }
                }
            return true;
            }
            catch (Exception)
            {
            }

            log.Write("MAIN-Failed to read senzor data :(");
            return false;
        }
        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    wsConector.Check();
                    syncer.Start();
                    break;

                case PowerModes.Suspend:
                    syncer.Stop();
                    wsConector.disconnect();
                    break;
            }
        }
    }
}
