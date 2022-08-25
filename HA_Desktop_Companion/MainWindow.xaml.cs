﻿using System.Windows;
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

namespace HA_Desktop_Companion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string conigurationPath = @"/configuration.yaml";
        public static string logFilenPath = @"/log.txt";

        public static HAApi_v2 ApiConnectiom2;
        public static HAApi_Websocket WebsocketConnectiom;

        private static Logging log;
        Configuration configurationClass;
        public static Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>> configurationData;

        private bool settings_debug = false;
        private bool isRegistered = false;

        DispatcherTimer watchdogTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.SettingUpdate)
            {
                Properties.Settings.Default.Upgrade();
                //Properties.Settings.Default.Reload();
                Properties.Settings.Default.Reset();

                Properties.Settings.Default.SettingUpdate = false;
                Properties.Settings.Default.Save();
            }

            settings_debug = Properties.Settings.Default.debug;

            //Initialize Config and Log
            log = new Logging(Directory.GetCurrentDirectory() + logFilenPath);
            configurationClass = new Configuration(Directory.GetCurrentDirectory() + conigurationPath);
            log.Write("MAIN -> Initialization");

        }

        private static void AllUnhandledExceptions(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            MessageBox.Show(ex.ToString());
            log.Write(ex.ToString());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            log.Write("MAIN -> Loading");

            //Load Config
            if (!configurationClass.load())
            {
                log.Write("MAIN - failed to load configuration.yaml");
            }

            configurationData = configurationClass.GetConfigurationData();

            //Load Settings
            string decodedApiToken = Encryption.ToInsecureString(Encryption.DecryptString(Properties.Settings.Default.apiToken));
            string decodedWebhookId = Encryption.ToInsecureString(Encryption.DecryptString(Properties.Settings.Default.apiWebhookId));
            string base_url = Properties.Settings.Default.apiBaseUrl;
            string remote_ui_url = Properties.Settings.Default.apiRemoteUiUrl;
            string cloudhook_url = Properties.Settings.Default.apiCloudhookUrl;

            //Get Info for Device Registration
            string hostname = Dns.GetHostName() + "_DEBUG";
            string maufactorer = Sensors.queryWmic("Win32_ComputerSystem", "Manufacturer", @"\\root\CIMV2");
            string model = Sensors.queryWmic("Win32_ComputerSystem", "Model", @"\\root\CIMV2");
            string os = Sensors.queryWmic("Win32_OperatingSystem", "Caption", @"\\root\CIMV2");
            string osVersion = Environment.OSVersion.ToString();

            //Set UI
            debug.IsChecked = settings_debug;
            apiToken.Password = decodedApiToken;
            apiBaseUrl.Text = base_url;
            version.Content = Assembly.GetEntryAssembly().GetName().Version.ToString();
            Title = hostname;

            //check if inputs are not null
            if (String.IsNullOrEmpty(apiToken.Password) && String.IsNullOrEmpty(apiBaseUrl.Text))
            {
                log.Write("MAIN - Token od URL => Null");
            } else {
                //Ping the Server address
                int timeout = 50;
                do
                {
                    if (timeout <= 0)
                    {
                        MessageBox.Show("Unable to connect to API on URL:" + base_url);
                        registration.IsEnabled = true;
                        return;
                    }

                    System.Threading.Thread.Sleep(500);
                    registration.Content = "Connecting";
                    timeout--;
                } while (!HAcheckConection(base_url));

                //Initialize API Class
                ApiConnectiom2 = new HAApi_v2(base_url, decodedApiToken, log, decodedWebhookId, remote_ui_url, cloudhook_url);
                ApiConnectiom2.enableDebug(true);

                if (ApiConnectiom2.registerHaDevice(hostname.ToLower(), hostname, model, maufactorer, os, osVersion))
                {
                    //Start WatchDog Timer
                    StartMainThreadTicker();
                    isRegistered = true;
                    //Initialize Web Socket
                    WebsocketConnectiom = new HAApi_Websocket(base_url, decodedApiToken, log, decodedWebhookId, remote_ui_url, cloudhook_url);

                    //registration.IsEnabled = false;
                }
                else
                {
                    registration.IsEnabled = true;
                    isRegistered = false;
                }
            }
            log.Write("Main -> Loaded");
        }
        
        private static bool HAcheckConection(string url)
        {
            //TODO: MOVE TO API class
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 5000;
            request.Method = "GET"; // As per Lasse's comment
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    log.Write("API -> connection Werified:" + url);
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException ex)
            {
                log.Write("API -> Failed to connect to:" + url + " " + ex.Message);
                return false;
            }
            return false;
        }

        private async void registrationAsync_Click(object sender, RoutedEventArgs e)
        {
            log.Write("MAIN -> Registration Button Clicked");

            //Disable data reporting if running
            if (watchdogTimer.IsEnabled)
            {
                watchdogTimer.Stop();
                log.Write("MAIN - Watchdog Stoped");
            }

            //check if inputs are not null
            if (String.IsNullOrEmpty(apiToken.Password) && String.IsNullOrEmpty(apiBaseUrl.Text))
            {
                log.Write("MAIN - Token od URL => Null");
                isRegistered = false;
            }
            else
            {
                //Load Config
                if (!configurationClass.load())
                {
                    log.Write("MAIN - failed to load configuration.yaml");
                }
                configurationData = configurationClass.GetConfigurationData();

                //Get Info for Device Registration
                string hostname = Dns.GetHostName() + "_DEBUG";
                string maufactorer = Sensors.queryWmic("Win32_ComputerSystem", "Manufacturer", @"\\root\CIMV2");
                string model = Sensors.queryWmic("Win32_ComputerSystem", "Model", @"\\root\CIMV2");
                string os = Sensors.queryWmic("Win32_OperatingSystem", "Caption", @"\\root\CIMV2");
                string osVersion = Environment.OSVersion.ToString();

                //Set UI
                Title = hostname;

                //Ping the Server address
                int timeout = 50;
                do
                {
                    if (timeout <= 0)
                    {
                        MessageBox.Show("Unable to connect to API on URL:" + apiBaseUrl.Text);
                        registration.IsEnabled = true;
                        return;
                    }

                    System.Threading.Thread.Sleep(500);
                    registration.Content = "Connecting";
                    timeout--;
                } while (!HAcheckConection(apiBaseUrl.Text));

                //Initialize API Class
                ApiConnectiom2 = new HAApi_v2(apiBaseUrl.Text, apiToken.Password, log);
                ApiConnectiom2.enableDebug(true);

                if (ApiConnectiom2.registerHaDevice(hostname.ToLower(), hostname, model, maufactorer, os, osVersion))
                {
                    await RegisterApiDataAsync(ApiConnectiom2);

                    //Save Settings
                    Properties.Settings.Default.apiBaseUrl = apiBaseUrl.Text;
                    Properties.Settings.Default.apiToken = Encryption.EncryptString(Encryption.ToSecureString(apiToken.Password));
                    Properties.Settings.Default.apiWebhookId = Encryption.EncryptString(Encryption.ToSecureString(ApiConnectiom2.api_webhook_id));
                    Properties.Settings.Default.apiRemoteUiUrl = ApiConnectiom2.api_remote_ui_url;
                    Properties.Settings.Default.apiCloudhookUrl = ApiConnectiom2.api_cloudhook_url;
                    Properties.Settings.Default.Save();

                    //Register WatchDog Timer
                    RegisterAutostart();
                    StartMainThreadTicker();
                    isRegistered = true;


                    //Initialize Web Socket
                    WebsocketConnectiom = new HAApi_Websocket(apiBaseUrl.Text, apiToken.Password, log, ApiConnectiom2.api_webhook_id, ApiConnectiom2.api_remote_ui_url, ApiConnectiom2.api_cloudhook_url);

                    //Modifi UI
                    registration.IsEnabled = false;
                    registration.Content = "Connected";

                }
                else
                {
                    registration.IsEnabled = true;
                }
            }

            log.Write("MAIN -> Registration Action Done");
        }

        private static void RegisterAutostart()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string exePath = Path.Combine(assemblyFolder, "HA_Desktop_Companion.exe");
                key.SetValue("HA_Desktop_Companion", "\"" + exePath + "\"");
                key.Close();
            }
        }

        public void StartMainThreadTicker()
        {
            log.Write("MAIN -> Registtering Watch dock caller");

            watchdogTimer.Tick += new EventHandler(MainThreadTickAsync);
            watchdogTimer.Interval = new TimeSpan(0, 0, 10);
            watchdogTimer.Start();

            registration.Content = "Registered";
        }

        private async void MainThreadTickAsync(object sender, EventArgs e)
        {
            log.Write("MAIN -> Watchdog tick !");

            //TODO: Do not discover battery id PC

            if (isRegistered)
            {
                await sendApiDataParallelAsync(ApiConnectiom2);

            }

            WebsocketConnectiom.Check();

            //ApiConnectiom.HASendSenzorLocation();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            log.Write("MAIN -> closing to tray");

            var app = Application.Current as App;
            app.ShowNotification(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, "App keeps Running in background!");

            this.ShowInTaskbar = false;
            this.Hide();

            e.Cancel = true;
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            log.Write("MAIN -> Exit");

            Environment.Exit(0);
        }

        private void debug_Checked(object sender, RoutedEventArgs e)
        {
            settings_debug = debug.IsChecked ?? false;
            Properties.Settings.Default.debug = settings_debug;
            Properties.Settings.Default.Save();
        }

        private bool getEntityData(HAApi_v2 ApiConnection, string integration, Type sensorsClass, Dictionary<string, string> sensor, string sensorType)
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
                    //TODO Round if INT
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
        
        private async Task sendApiDataParallelAsync(HAApi_v2 ApiConnection)
        {
            var sensorsClass = typeof(Sensors);
            Dictionary<string, object> senzorTypes = new Dictionary<string, object>();
            senzorTypes.Add("sensor", configurationData["sensor"]);
            List<Task<bool>> tasks = new List<Task<bool>>();

            //Add Binary Senzors to Dictionary
            if (configurationData.ContainsKey("binary_sensor"))
                senzorTypes.Add("binary_sensor", configurationData["binary_sensor"]);

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
            /*foreach (var item in results)
            {
                //Debug.WriteLine((string) item.ToString());
            }*/

            //Send Data
            ApiConnection.sendHaEntitiData();
        }

        private async Task RegisterApiDataAsync(HAApi_v2 ApiConnection)
        {
            log.Write("MAIN -> Entity Registration");


            //Register Senzors
            Dictionary<string, object> senzorTypes = new Dictionary<string, object>();
            senzorTypes.Add("sensor", configurationData["sensor"]);

            if (configurationData.ContainsKey("binary_sensor"))
                senzorTypes.Add("binary_sensor", configurationData["binary_sensor"]);

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

                           ApiConnectiom2.registerHaEntiti(senzor["unique_id"], senzor["name"], defaultValue, senzorType, deviceClass, entityCategory, icon, unitOfMeasurement);
                        }
                    }
                }
            }

            log.Write("MAIN -> Entity Registration Done");

        }
    }
}
