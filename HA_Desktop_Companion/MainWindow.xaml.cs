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


namespace HA_Desktop_Companion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string ConigurationPath = @".\configuration.yaml";
        public static HAApi ApiConnectiom;
        public static HAApi_Websocket WebsocketConnectiom;
        public bool debugEnabled = false;

        public static Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>> configuration;

        public MainWindow()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += AllUnhandledExceptions;
            var path = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            MessageBox.Show(path);

            if (Properties.Settings.Default.SettingUpdate)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.Reload();
                Properties.Settings.Default.SettingUpdate = false;
                Properties.Settings.Default.Save();
            }


        }

        private static void AllUnhandledExceptions(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            MessageBox.Show(ex.ToString());
            using (StreamWriter sw = File.AppendText(@"C:\Users\JonatanRek\Desktop\log.txt"))
            {
                sw.WriteLine(JsonSerializer.Serialize(ex.ToString()));
            }
            //Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException(ex));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Load Config
            Configuration configurationClass = new Configuration(ConigurationPath);
            configuration = configurationClass.GetConfigurationData();

            /*
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                WindowsPrincipal currentPrincipal = (WindowsPrincipal) Thread.CurrentPrincipal;

                if (currentPrincipal.IsInRole("Administrators"))
                {
                    // continue programm
                }
                else
                {
                    // throw exception/show errorMessage - exit programm
                }
             */

            string decodedApiToken = Encryption.ToInsecureString(Encryption.DecryptString(Properties.Settings.Default.apiToken));
            string decodedWebhookId = Encryption.ToInsecureString(Encryption.DecryptString(Properties.Settings.Default.apiWebhookId));
            string base_url = Properties.Settings.Default.apiBaseUrl;
            string remote_ui_url = Properties.Settings.Default.apiRemoteUiUrl;
            string cloudhook_url = Properties.Settings.Default.apiCloudhookUrl;
            debugEnabled = Properties.Settings.Default.debug;


            apiToken.Password = decodedApiToken;
            apiBaseUrl.Text = base_url;

            if (decodedWebhookId != "")
            {
                try
                {
                    ApiConnectiom = new HAApi(base_url, decodedApiToken, decodedWebhookId, remote_ui_url, cloudhook_url);
                    WebsocketConnectiom = new HAApi_Websocket(base_url, decodedApiToken, decodedWebhookId, remote_ui_url, cloudhook_url);

                    string hostname = System.Net.Dns.GetHostName() + "";
                    Title = hostname;

                    StartWatchdog();

                    //registration.IsEnabled = false;
                }
                catch (Exception)
                {
                    //registration.IsEnabled = true;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var app = Application.Current as App;
            app.ShowNotification(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, "App keeps Running in background!");
            this.ShowInTaskbar = false;
            this.Hide();
            e.Cancel = true;
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void registration_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(apiBaseUrl.Text) || String.IsNullOrEmpty(apiToken.Password))
            {
                return;
            }

            string hostname = System.Net.Dns.GetHostName() + "";
            string maufactorer = Sensors.queryWmic("Win32_ComputerSystem", "Manufacturer", @"\\root\CIMV2");
            string model = Sensors.queryWmic("Win32_ComputerSystem", "Model", @"\\root\CIMV2");
            string os = Sensors.queryWmic("Win32_OperatingSystem", "Caption", @"\\root\CIMV2");

            Title = hostname;

            ApiConnectiom = new HAApi(apiBaseUrl.Text, apiToken.Password, hostname.ToLower(), hostname, model, maufactorer, os, Environment.OSVersion.ToString());

            WebsocketConnectiom = new HAApi_Websocket(apiBaseUrl.Text, apiToken.Password, ApiConnectiom.webhook_id);

            SaveSettings();
            SenzorRegistration();
            RegisterAutostart();
            StartWatchdog();

            //registration.IsEnabled = false;
        }

        private void SaveSettings()
        {
            Properties.Settings.Default.apiBaseUrl = apiBaseUrl.Text;
            Properties.Settings.Default.apiToken = Encryption.EncryptString(Encryption.ToSecureString(apiToken.Password));
            Properties.Settings.Default.apiWebhookId = Encryption.EncryptString(Encryption.ToSecureString(ApiConnectiom.webhook_id));
            Properties.Settings.Default.apiRemoteUiUrl = ApiConnectiom.remote_ui_url;
            Properties.Settings.Default.apiCloudhookUrl = ApiConnectiom.cloudhook_url;

            Properties.Settings.Default.Save();
        }

        private static void SenzorRegistration()
        {
            //Register Senzors
            Dictionary<string, object> senzorTypes = new Dictionary<string, object>();
            senzorTypes.Add("sensor", configuration["sensor"]);

            if (configuration.ContainsKey("binary_sensor"))
                senzorTypes.Add("binary_sensor", configuration["binary_sensor"]);

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

                            //MessageBox.Show(JsonSerializer.Serialize(senzor));

                            //MessageBox.Show(senzor["name"]);
                            string device_class = "";
                            if (senzor.ContainsKey("device_class"))
                                device_class = senzor["device_class"];

                            if (Int32.Parse(Sensors.queryWmic("Win32_ComputerSystem", "PCSystemType", @"\\root\CIMV2")) != 2 && device_class == "battery")
                                continue;

                            string icon = "";
                            if (senzor.ContainsKey("icon"))
                                icon = senzor["icon"];

                            string unit_of_measurement = "";
                            if (senzor.ContainsKey("unit_of_measurement"))
                                unit_of_measurement = senzor["unit_of_measurement"];

                            string entity_category = "";
                            if (senzor.ContainsKey("entity_category"))
                                entity_category = senzor["entity_category"];

                            if (senzorType == "binary_sensor")
                            {
                                ApiConnectiom.HASenzorRegistration(senzor["unique_id"], senzor["name"], false, device_class, unit_of_measurement, icon, entity_category);
                            }
                            else
                            {
                                ApiConnectiom.HASenzorRegistration(senzor["unique_id"], senzor["name"], 0, device_class, unit_of_measurement, icon, entity_category);
                            }
                            Debug.WriteLine(senzor["unique_id"] + " - " + "Sensor Sucesfully Loadet");
                        }
                    }
                }
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //TODO: Do not discover battery id PC

            var sensorsClass = typeof(Sensors);
            Dictionary<string, object> senzorTypes = new Dictionary<string, object>();
            senzorTypes.Add("sensor", configuration["sensor"]);

            if (configuration.ContainsKey("binary_sensor"))
                senzorTypes.Add("binary_sensor", configuration["binary_sensor"]);

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

                            object sensorData = null;
                            if (integration.Key == "wmic")
                            {
                                if (senzor.ContainsKey("wmic_path") && senzor.ContainsKey("wmic_selector") && senzor.ContainsKey("wmic_namespace"))
                                {
                                    string methodName = "query" + integration.Key[0].ToString().ToUpper() + integration.Key.Substring(1);
                                    sensorData = sensorsClass.GetMethod(methodName).Invoke(this, new[] { senzor["wmic_path"], senzor["wmic_selector"], senzor["wmic_namespace"] });
                                }
                                else if (integration.Key == "consent_store")
                                {
                                    if (senzor.ContainsKey("consent_category") && senzor.ContainsKey("wmic_selector") && senzor.ContainsKey("wmic_namespace"))
                                    {
                                        string methodName = "query";
                                        foreach (var methodNameSegment in integration.Key.Split("_"))
                                        {
                                            methodName += methodNameSegment.ToString().ToUpper() + methodNameSegment.Substring(1);
                                        }
                                        sensorData = sensorsClass.GetMethod(methodName).Invoke(this, new[] { senzor["consent_category"] });
                                    }
                                }
                                else if (integration.Key == "uptime")
                                {
                                    string methodName = "queryUptime";
                                    sensorData = sensorsClass.GetMethod(methodName).Invoke(this, null);
                                }
                                else if (integration.Key == "current_window")
                                {
                                    string methodName = "queryCurrentWindow";
                                    sensorData = sensorsClass.GetMethod(methodName).Invoke(this, null);
                                }
                                else if (integration.Key == "wifi")
                                {
                                    string methodName = "queryWifi";
                                    sensorData = sensorsClass.GetMethod(methodName).Invoke(this, new[] { senzor["selector"], senzor["deselector"] });
                                }

                                if (sensorData != null)
                                {
                                    if (senzor.ContainsKey("value_map"))
                                    {
                                        MessageBox.Show(sensorData.ToString());
                                        //Dictionary<string, string> valueMap = senzor["value_map"].Select(item => item.Split('|')).ToDictionary(s => s[0], s => s[1]);
                                        string[] valueMap = senzor["value_map"].Split("|");
                                        //check if key exist in map 
                                        sensorData = valueMap[(Int32.Parse((sensorData).ToString()))];
                                        MessageBox.Show(sensorData.ToString());
                                    }
                                    ApiConnectiom.HASendSenzorData(senzor["unique_id"], Sensors.convertToType(sensorData));
                                }
                            }
                        }
                    }
                }
            }
            WebsocketConnectiom.Check();
            //ApiConnectiom.HASendSenzorLocation();
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

        public void StartWatchdog()
        {
            if (debugEnabled)
            {
                ApiConnectiom.enableDebug();
            }

            DispatcherTimer watchdogTimer = new DispatcherTimer();
            watchdogTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            watchdogTimer.Interval = new TimeSpan(0, 0, 10);
            watchdogTimer.Start();

            registration.Content = "Registered";
        }

        private void debug_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.debug = debug.IsChecked ?? false;

            Properties.Settings.Default.Save();
        }

        /*string[] drives = Environment.GetLogicalDrives();
        Console.WriteLine("GetLogicalDrives: {0}", String.Join(", ", drives));*/
    }
}
