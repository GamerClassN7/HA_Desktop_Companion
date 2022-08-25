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
            log.Write("MAIN ->CLOSING");
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

            registration.Content = "Registered";


        }

        private void quit_Click(object sender, RoutedEventArgs e)
        {
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
            log.Write("MAIN-Syncer Tick n:" + syncerIterator);

            await sendApiDataParallelAsync(apiConector);

            syncerIterator++;
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
                //Debug.WriteLine((string) item.ToString());
            }

            //Send Data
            ApiConnection.sendHaEntitiData();
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


        /* public static string conigurationPath = @"/configuration.yaml";
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
                 registration.IsEnabled = false;


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

         }

         private void debug_Checked(object sender, RoutedEventArgs e)
         {
             settings_debug = debug.IsChecked ?? false;
             Properties.Settings.Default.debug = settings_debug;
             Properties.Settings.Default.Save();
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
             }

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

         }*/
    }
    }
