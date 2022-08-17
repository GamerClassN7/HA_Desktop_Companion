using System.Net.Http;
using System.Windows;
using System.Collections.Generic;
using System.Net.Http.Json;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reflection;
using System.Windows.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
        public static HAApi ApiConnectiom;

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
            this.WindowState = WindowState.Minimized;
            e.Cancel = true;
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

            RegisterAutostart();

            if (Int32.Parse(Sensors.queryWMIC("Win32_ComputerSystem", "PCSystemType", @"\\root\CIMV2")) == 2)
            {
                ApiConnectiom.HASenzorRegistration("battery_level", "Battery Level", 0, "battery", "%", "mdi:battery", "diagnostic");
                ApiConnectiom.HASenzorRegistration("battery_state", "Battery State", "Unknown", "battery", "", "mdi:battery-minus", "diagnostic");
                ApiConnectiom.HASenzorRegistration("is_charging", "Is Charging", false, "plug", "", "mdi:power-plug-off", "diagnostic");
            }

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



            StartWatchdog();
            //registration.IsEnabled = false;
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

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {

            //Make Use of reflections
            //var type = Type.GetType(type_name);
            //ApiConnectiom.HASendSenzorLocation();

            if (Int32.Parse(Sensors.queryWMIC("Win32_ComputerSystem", "PCSystemType", @"\\root\CIMV2")) == 2)
            {
                ApiConnectiom.HASendSenzorData("battery_level", GetBatteryPercent());
                ApiConnectiom.HASendSenzorData("battery_state", GetBatteryStatus().ToString());
                ApiConnectiom.HASendSenzorData("is_charging", GetPowerLineStatus());
            }

            ApiConnectiom.HASendSenzorData("wifi_ssid", getWifiSSID().ToString());
            ApiConnectiom.HASendSenzorData("currently_active_window", Sensors.queryActiveWindowTitle());

            ApiConnectiom.HASendSenzorData("camera_in_use", Sensors.queryConsetStore("webcam"));
            ApiConnectiom.HASendSenzorData("microphone_in_use", Sensors.queryConsetStore("microphone"));
            ApiConnectiom.HASendSenzorData("location_in_use", Sensors.queryConsetStore("location"));

            ApiConnectiom.HASendSenzorData("cpu_temp", getCPUTemperature());
            ApiConnectiom.HASendSenzorData("cpu_usage", GetCPUUsagePercent());
            ApiConnectiom.HASendSenzorData("free_ram", GetFreeRam());

            ApiConnectiom.HASendSenzorData("uptime", (int) Sensors.queryMachineUpTime().TotalSeconds);
            ApiConnectiom.HASendSenzorData("update_available", (false).ToString());
        }

        public static double GetBatteryPercent()
        {
            try { 
                return Int32.Parse(Sensors.queryWMIC("Win32_Battery", "EstimatedChargeRemaining", @"\\root\CIMV2"));
            }
            catch (Exception)
            {
            }
            return 0;

        }

        public static string GetBatteryStatus()
        {
            try
            {
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
                    return StatusCodes[state];
                }
            }
            catch (Exception)
            {
            }
            return "Unknown";
        }

        public static bool GetPowerLineStatus()
        {
            try
            {
                return Boolean.Parse(Sensors.queryWMIC("BatteryStatus", "PowerOnline", @"\\root\wmi"));
               
            }
            catch (Exception)
            {
            }

            return false;
        }

        private string getWifiSSID()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = {
                    FileName = "netsh.exe",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                    }
                };
                process.Start();

                foreach (var item in process.StandardOutput.ReadToEnd().ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
                {
                    if (item.Contains("SSID") && !item.Contains("BSSID"))
                    {
                        return item;
                    }
                }
            }
            catch (Exception)
            {
            }
            return "Not available";
        }

        private double getCPUTemperature() {
            try
            {
                return (Math.Round(Int32.Parse(Sensors.queryWMIC("Win32_PerfFormattedData_Counters_ThermalZoneInformation.Name=\"\\\\_TZ.CPUZ\"", "Temperature", @"\\root\CIMV2")) - 273.15, 2));
            }
            catch (Exception)
            {
                try
                {
                    return (Math.Round(Int32.Parse(Sensors.queryWMIC("Win32_PerfFormattedData_Counters_ThermalZoneInformation.Name=\"\\\\_TZ.THM0\"", "Temperature", @"\\root\CIMV2")) - 273.15, 2));
                }
                catch (Exception)
                {
                }
            }

            return 0;

        }

        private double GetCPUUsagePercent()
        {
            try
            {
                return (Convert.ToInt32(Int16.Parse(Sensors.queryWMIC("Win32_Processor", "LoadPercentage", @"\\root\CIMV2"))));
            }
            catch (Exception)
            {
            }
            return 0;
        }

        private double GetFreeRam()
        {
            // kilobytes
            try
            {
                return Int32.Parse(Sensors.queryWMIC("Win32_OperatingSystem", "FreePhysicalMemory", @"\\root\CIMV2"));
            }
            catch (Exception)
            {
            }
            return 0;
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

        private void close_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
