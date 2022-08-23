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

namespace HA_Desktop_Companion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Logging log = new Logging(".\\log.txt");
        public static string ConigurationPath = @".\configuration.yaml";
        public static HAApi_v2 ApiConnectiom2;
        public static HAApi_Websocket WebsocketConnectiom;

        public static Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>> configuration;

        private bool settings_debug = false;

        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.SettingUpdate)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.Reload();
                Properties.Settings.Default.SettingUpdate = false;
                Properties.Settings.Default.Save();
            }

            settings_debug = Properties.Settings.Default.debug;
        }

        private static void AllUnhandledExceptions(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            MessageBox.Show(ex.ToString());
            using (StreamWriter sw = File.AppendText(@"C:\Users\JonatanRek\Desktop\log.txt"))
            {
                sw.WriteLine(JsonSerializer.Serialize(ex.ToString()));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Load Config
            Configuration configurationClass = new Configuration(ConigurationPath);
            configuration = configurationClass.GetConfigurationData();

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
            Title = hostname;

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
            ApiConnectiom2 = new HAApi_v2(base_url, decodedApiToken, decodedWebhookId, remote_ui_url, cloudhook_url);
            ApiConnectiom2.enableDebug(true);

            if (ApiConnectiom2.registerHaDevice(hostname.ToLower(), hostname, model, maufactorer, os, osVersion))
            {
                //Start WatchDog Timer
                StartMainThreadTicker();

                //Initialize Web Socket
                WebsocketConnectiom = new HAApi_Websocket(base_url, decodedApiToken, decodedWebhookId, remote_ui_url, cloudhook_url);

                //registration.IsEnabled = false;
            }
            else
            {
                registration.IsEnabled = true;
            }
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

        private void registration_Click(object sender, RoutedEventArgs e)
        {
            //Werifi that Inputs are Set
            if (String.IsNullOrEmpty(apiBaseUrl.Text) || String.IsNullOrEmpty(apiToken.Password))
                return;

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
            ApiConnectiom2 = new HAApi_v2(apiBaseUrl.Text, apiToken.Password);
            ApiConnectiom2.enableDebug(true);

            if (ApiConnectiom2.registerHaDevice(hostname.ToLower(), hostname, model, maufactorer, os, osVersion))
            {
                SensorRegistration();

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

                //Initialize Web Socket
                WebsocketConnectiom = new HAApi_Websocket(apiBaseUrl.Text, apiToken.Password, ApiConnectiom2.api_webhook_id, ApiConnectiom2.api_remote_ui_url, ApiConnectiom2.api_cloudhook_url);

                //Modifi UI
                registration.IsEnabled = false;
            }
            else
            {
                registration.IsEnabled = true;
            }
        }
        private static void SensorRegistration()
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

                            if (senzorType == "binary_sensor")
                            {
                                ApiConnectiom2.registerHaEntiti(senzor["unique_id"], senzor["name"], false, senzorType, deviceClass, entityCategory, icon, unitOfMeasurement);
                            }
                            else
                            {
                                ApiConnectiom2.registerHaEntiti(senzor["unique_id"], senzor["name"], 0, senzorType, deviceClass, entityCategory, icon, unitOfMeasurement);
                            }

                        }
                    }
                }
            }
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
            DispatcherTimer watchdogTimer = new DispatcherTimer();
            watchdogTimer.Tick += new EventHandler(MainThreadTick);
            watchdogTimer.Interval = new TimeSpan(0, 0, 10);
            watchdogTimer.Start();

            registration.Content = "Registered";
        }

        private void MainThreadTick(object sender, EventArgs e)
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
                            string methodName = "query";

                            foreach (var methodNameSegment in integration.Key.Split("_"))
                            {
                                methodName += methodNameSegment[0].ToString().ToUpper() + methodNameSegment.Substring(1);
                            }
                            
                            MethodInfo method = sensorsClass.GetMethod(methodName);
                            if (method == null)
                                continue;

                            ParameterInfo[] pars = method.GetParameters();
                            List<object> parameters = new List<object>();

                            foreach (ParameterInfo p in pars)
                            {
                                if (senzor.ContainsKey(p.Name))
                                {
                                    parameters.Insert(p.Position, senzor[p.Name]);
                                }
                                else if (p.IsOptional)
                                {
                                    parameters.Insert(p.Position, p.DefaultValue);
                                }
                            }

                            sensorData = method.Invoke(this, parameters.ToArray());

                            if (sensorData != null)
                            {
                                if (senzor.ContainsKey("value_map"))
                                {
                                    string[] valueMap = senzor["value_map"].Split("|");
                                    sensorData = valueMap[(Int32.Parse((sensorData).ToString()))];
                                }

                                sensorData = Sensors.convertToType(sensorData);

                                if (senzorType == "binary_sensor")
                                {
                                    ApiConnectiom2.addHaEntitiData(senzor["unique_id"], sensorData, "binary_sensor");
                                }
                                else
                                {
                                    ApiConnectiom2.addHaEntitiData(senzor["unique_id"], sensorData);
                                }
                            }
                        }
                    }
                }
            }
            ApiConnectiom2.sendHaEntitiData();
            WebsocketConnectiom.Check();
            //ApiConnectiom.HASendSenzorLocation();
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

        private void debug_Checked(object sender, RoutedEventArgs e)
        {
            settings_debug = debug.IsChecked ?? false;
            Properties.Settings.Default.debug = settings_debug;
            Properties.Settings.Default.Save();
        }

        /*string[] drives = Environment.GetLogicalDrives();
        Console.WriteLine("GetLogicalDrives: {0}", String.Join(", ", drives));*/
    }
}
