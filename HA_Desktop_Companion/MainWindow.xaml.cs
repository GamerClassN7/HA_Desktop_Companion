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
        public static Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>> configuration;

        public MainWindow()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += AllUnhandledExceptions;
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

            apiToken.Password = decodedApiToken;
            apiBaseUrl.Text = base_url;

            if (decodedWebhookId != "")
            {
                try
                {
                    ApiConnectiom = new HAApi(base_url, decodedApiToken, decodedWebhookId, remote_ui_url, cloudhook_url);
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
            this.Hide();
            e.Cancel = true;
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void registration_Click(object sender, RoutedEventArgs e)
        {

            string hostname = System.Net.Dns.GetHostName() + "";
            string maufactorer = Sensors.queryWMIC("Win32_ComputerSystem", "Manufacturer", @"\\root\CIMV2");
            string model = Sensors.queryWMIC("Win32_ComputerSystem", "Model", @"\\root\CIMV2");
            string os = Sensors.queryWMIC("Win32_OperatingSystem", "Caption", @"\\root\CIMV2");

            Title = hostname;

            ApiConnectiom = new HAApi(apiBaseUrl.Text, apiToken.Password, hostname.ToLower(), hostname, model, maufactorer, os, Environment.OSVersion.ToString());

            Properties.Settings.Default.apiBaseUrl = apiBaseUrl.Text;
            Properties.Settings.Default.apiToken = Encryption.EncryptString(Encryption.ToSecureString(apiToken.Password));
            Properties.Settings.Default.apiWebhookId = Encryption.EncryptString(Encryption.ToSecureString(ApiConnectiom.webhook_id));
            Properties.Settings.Default.apiRemoteUiUrl = ApiConnectiom.remote_ui_url;
            Properties.Settings.Default.apiCloudhookUrl = ApiConnectiom.cloudhook_url;

            Properties.Settings.Default.Save();

            

            //Register Senzors
            Dictionary<string, object> senzorTypes = new Dictionary<string, object>();
            senzorTypes.Add("sensor", configuration["sensor"]);

            if (configuration.ContainsKey("binary_sensor"))
                senzorTypes.Add("binary_sensor", configuration["binary_sensor"]);

            foreach (var item in senzorTypes)
            {
                string senzorType = item.Key;
                Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>> platforms = (Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>) senzorTypes[senzorType];
                foreach (var platform in platforms)
                {
                    Dictionary<string, List<Dictionary<string, string>>> integrations = (Dictionary<string, List<Dictionary<string, string>>>) platform.Value;
                    foreach (var integration in integrations)
                    {
                        List<Dictionary<string, string>> senzors = (List<Dictionary<string, string>>) integration.Value;
                        foreach (var senzor in senzors)
                        {

                            //MessageBox.Show(JsonSerializer.Serialize(senzor));

                            //MessageBox.Show(senzor["name"]);
                            string device_class = "";
                            if (senzor.ContainsKey("device_class"))
                                device_class = senzor["device_class"];

                            if (Int32.Parse(Sensors.queryWMIC("Win32_ComputerSystem", "PCSystemType", @"\\root\CIMV2")) != 2 && device_class == "battery")
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

                            if (senzorType == "binary_sensor") { 
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

            System.Threading.Thread.Sleep(3000);


            /* if (Int32.Parse(Sensors.queryWMIC("Win32_ComputerSystem", "PCSystemType", @"\\root\CIMV2")) == 2)
             {
                 ApiConnectiom.HASenzorRegistration("battery_level", "Battery Level", 0, "battery", "%", "mdi:battery", "diagnostic");
                 ApiConnectiom.HASenzorRegistration("battery_state", "Battery State", "Unknown", "battery", "", "mdi:battery-minus", "diagnostic");
                 ApiConnectiom.HASenzorRegistration("is_charging", "Is Charging", false, "plug", "", "mdi:power-plug-off", "diagnostic");
             }

             ApiConnectiom.HASenzorRegistration("wifi_state", "Wifi State", "Unknown", "", "", "mdi:wifi", "");
             ApiConnectiom.HASenzorRegistration("wifi_ssid", "Wifi SSID", "Unknown", "", "", "mdi:wifi", "");
             ApiConnectiom.HASenzorRegistration("currently_active_window", "Currently Active Window", "Unknown", "", "", "mdi:application", "");

             ApiConnectiom.HASenzorRegistration("camera_in_use", "Camera in use", false, "", "", "mdi:camera", "");
             ApiConnectiom.HASenzorRegistration("microphone_in_use", "Microphone in use", false, "", "", "mdi:microphone", "");
             ApiConnectiom.HASenzorRegistration("location_in_use", "Location in use", false, "", "", "mdi:crosshairs-gps", "");

             ApiConnectiom.HASenzorRegistration("cpu_temp", "CPU Temperature", 0, "", "°C", "mdi:cpu-64-bit", "diagnostic");
             ApiConnectiom.HASenzorRegistration("cpu_usage", "CPU Usage", 0, "", "%", "mdi:cpu-64-bit", "diagnostic");
             ApiConnectiom.HASenzorRegistration("free_ram", "Free Ram", 0, "", "kilobytes", "mdi:clock", "diagnostic");

             ApiConnectiom.HASenzorRegistration("uptime", "Uptime", 0, "", "s", "mdi:timer-outline", "diagnostic");
             ApiConnectiom.HASenzorRegistration("update_available", "Update Availible", false, "firmware", "", "mdi:package", "diagnostic");
            */

            RegisterAutostart();
            StartWatchdog();
            //registration.IsEnabled = false;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {

            //Make Use of reflections
            //var type = Type.GetType(type_name);
            //ApiConnectiom.HASendSenzorLocation();

            if (Int32.Parse(Sensors.queryWMIC("Win32_ComputerSystem", "PCSystemType", @"\\root\CIMV2")) == 2)
            {
                var batterypercent = Sensors.queryWMIC("Win32_Battery", "EstimatedChargeRemaining", @"\\root\CIMV2");
                ApiConnectiom.HASendSenzorData("battery_level", Sensors.convertToType(batterypercent));

                var batterystate = "Unknown";
                Dictionary<int, string> StatusCodes = new Dictionary<int, string>();
                StatusCodes.Add(1, "Discharging");
                StatusCodes.Add(2, "On AC");
                StatusCodes.Add(3, "Fully Charged");
                StatusCodes.Add(4, "Low");
                StatusCodes.Add(5, "Critical");
                StatusCodes.Add(6, "Charging");
                StatusCodes.Add(7, "Charging and High");
                StatusCodes.Add(8, "Charging and Low");
                StatusCodes.Add(9, "Undefined");
                StatusCodes.Add(10, "Partially Charged");

                int state = Int32.Parse(Sensors.queryWMIC("Win32_Battery", "BatteryStatus", @"\\root\CIMV2"));
                if (state <= StatusCodes.Count)
                {
                    batterystate = StatusCodes[state];
                }
                ApiConnectiom.HASendSenzorData("battery_state", Sensors.convertToType(batterystate));

                var PowerLineStatus = Sensors.queryWMIC("BatteryStatus", "PowerOnline", @"\\root\wmi");
                ApiConnectiom.HASendSenzorData("is_charging", Sensors.convertToType(PowerLineStatus));
            }

            var wifistate = Sensors.queryWifi("SSID", "BSSID");
            ApiConnectiom.HASendSenzorData("wifi_state", Sensors.convertToType(wifistate));

            var wifissid = Sensors.queryWifi("State");
            ApiConnectiom.HASendSenzorData("wifi_ssid", Sensors.convertToType(wifissid));

            var windowname = Sensors.queryActiveWindowTitle();
            ApiConnectiom.HASendSenzorData("currently_active_window", Sensors.convertToType(windowname));

            var cameraConsent = Sensors.queryConsetStore("webcam");
            ApiConnectiom.HASendSenzorData("camera_in_use", Sensors.convertToType(cameraConsent));

            var microphoneConsent = Sensors.queryConsetStore("microphone");
            ApiConnectiom.HASendSenzorData("microphone_in_use", Sensors.convertToType(microphoneConsent));

            var locationConsent = Sensors.queryConsetStore("location");
            ApiConnectiom.HASendSenzorData("location_in_use", Sensors.convertToType(locationConsent));

            var cpuTemp = (Math.Round(Int32.Parse(Sensors.queryWMIC("Win32_PerfFormattedData_Counters_ThermalZoneInformation.Name=\"\\\\_TZ.CPUZ\"", "Temperature", @"\\root\CIMV2")) - 273.15, 2));
            ApiConnectiom.HASendSenzorData("cpu_temp", Sensors.convertToType(cpuTemp));

            var cpuUsage = Sensors.queryWMIC("Win32_Processor", "LoadPercentage", @"\\root\CIMV2");
            ApiConnectiom.HASendSenzorData("cpu_usage", Sensors.convertToType(cpuUsage));

            var ramFree = Sensors.queryWMIC("Win32_OperatingSystem", "FreePhysicalMemory", @"\\root\CIMV2");
            ApiConnectiom.HASendSenzorData("free_ram", Sensors.convertToType(ramFree));

            ApiConnectiom.HASendSenzorData("uptime", Sensors.queryMachineUpTime().TotalSeconds);
            ApiConnectiom.HASendSenzorData("update_available", (false));
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
            if ((debug.IsChecked ?? false))
            {
                ApiConnectiom.enableDebug();
            }

            DispatcherTimer watchdogTimer = new DispatcherTimer();
            watchdogTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            watchdogTimer.Interval = new TimeSpan(0, 0, 10);
            watchdogTimer.Start();

            registration.Content = "Registered";
        }

        /*string[] drives = Environment.GetLogicalDrives();
        Console.WriteLine("GetLogicalDrives: {0}", String.Join(", ", drives));*/
    }
}
