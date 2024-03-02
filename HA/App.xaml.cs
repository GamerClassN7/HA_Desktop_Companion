using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;
using HA.Class.Helpers;
using HA.Class.HomeAssistant;
using HA.Class.HomeAssistant.Objects;
using HA.Class.Sensors;
using HA.Class.YamlConfiguration;
using Forms = System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.Linq;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Security.Policy;
using AutoUpdaterDotNET;
using System.Diagnostics.Tracing;
using Windows.UI.Notifications;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Animation;
using System.Windows.Input;

namespace HA
{
    /// <summary>
    /// Interaction logic for App.xaml test 
    /// </summary>
    public partial class App : Application
    {
        private static bool connectionError = false;
        static DispatcherTimer? update = null;
        static Dictionary<string, DateTime> sensorUpdatedAtList = new Dictionary<string, DateTime>();
        static Dictionary<string, dynamic> sensorLastValues = new Dictionary<string, dynamic>();

        public HomeAssistantAPI ha;
        public HomeAssistantWS ws;

#if DEBUG
        public static string exeFullName = System.Reflection.Assembly.GetExecutingAssembly().Location;
        public static string appDir = System.IO.Path.GetDirectoryName(exeFullName);
#else
        public static string appDir = AppDomain.CurrentDomain.BaseDirectory;
#endif

        private static YamlConfiguration configurationObject = new YamlConfiguration(appDir + "/configuration.yaml");
        private static Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, dynamic>>>>> configData;

        private MainWindow mw;
        private Forms.NotifyIcon notifyIcon;

        public App()
        {
            notifyIcon = new Forms.NotifyIcon();
        }

        private void OnPowerChange(object s, PowerModeChangedEventArgs e, string ip = "8.8.8.8")
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    while (!PingHost(ip))
                    {
                        Logger.write("Ping "+ ip + " fasle");
                    }

                    Logger.write("Ping " + ip + " true");
                    Start(true);
                    break;

                case PowerModes.Suspend:
                    Close(true);
                    break;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AutoUpdater.Start("https://github.com/GamerClassN7/HA_Desktop_Companion/releases/latest/download/meta.xml");
            AutoUpdater.Synchronous = true;
            AutoUpdater.ShowRemindLaterButton = false;

            //NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            if (previousProcessDetected())
            {
                ShowNotification("Already Running !!!");
                Environment.Exit(0);
            }

            notifyIcon.Icon = new System.Drawing.Icon(appDir + "/ha_logo.ico");
            notifyIcon.Visible = true;
            notifyIcon.Text = System.AppDomain.CurrentDomain.FriendlyName;
            notifyIcon.DoubleClick += NotifyIcon_Click;

            notifyIcon.ContextMenuStrip = new Forms.ContextMenuStrip();

            notifyIcon.ContextMenuStrip.Items.Add("Home Assistant", null, OnHomeAssistant_Click);
            notifyIcon.ContextMenuStrip.Items.Add("Log", null, OnLog_Click);
            notifyIcon.ContextMenuStrip.Items.Add("Send Test Notification", null, OnTestNotification_Click);
            notifyIcon.ContextMenuStrip.Items.Add("Quit", null, OnQuit_Click);

            base.OnStartup(e);
        }

        private void OnLog_Click(object? sender, EventArgs e)
        {
            Process.Start("notepad", Logger.getLogPath());
        }

        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

        private void OnHomeAssistant_Click(object? sender, EventArgs e)
        {
            Process.Start("explorer", mw.url.Text);
        }

        private void OnQuit_Click(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void OnTestNotification_Click(object? sender, EventArgs e)
        {
            ShowNotification("Test","test");
        }

        private void NotifyIcon_Click(object? sender, EventArgs e)
        {
            MainWindow.Activate();
            MainWindow.ShowInTaskbar = true;
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose();

            base.OnExit(e);
        }

        public string GetRootDir()
        {
            return appDir;
        }

        public bool Start(bool sleepRecover = false)
        {
            Logger.init(appDir + "/log.log");

            string token = "";
            string url = "";
            string webhookId = "";
            string secret = "";
            Logger.write("Starting APP", 4);

            if (!sleepRecover)
            {
                mw = (MainWindow)Application.Current.MainWindow;
                
                //Clear check Buffers
                sensorLastValues.Clear();
                sensorUpdatedAtList.Clear();
            }
            else {
                Logger.write("HA Recovering Power suspend Mode!", 4);
            }

            //Load Config Yaml
            if (!configurationObject.LoadConfiguration())
            {
                MessageBox.Show("Config Error Report to Developer!");
            }
            configData = configurationObject.GetConfigurationData();

            //Load internal Config
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            token = config.AppSettings.Settings["token"].Value;
            url = config.AppSettings.Settings["url"].Value;
            webhookId = config.AppSettings.Settings["webhookId"].Value;
            secret = config.AppSettings.Settings["secret"].Value;
            
            //Initialize sleep Backup
            SystemEvents.PowerModeChanged += (sender, e) => OnPowerChange(sender, e, new Uri(url).Host);

            //Values for striping from log messages
            Logger.setSecreets(new string[] { token, url, webhookId, secret });

            try
            {
                ha = new HomeAssistantAPI(url, token);
            } catch (Exception ex)
            {
                Logger.write("HA Api initialization Failed!", 4);
                Logger.write(ex.Message, 4);
                mw.api_status.Foreground = new SolidColorBrush(Colors.Red);
                return false;
            }
            Logger.write("API initiualized", 4);


            try
            {
                Logger.write(("HA server Version" + ha.GetVersion()), 0);
            }
            catch (Exception ex)
            {
                Logger.write("Get HA version Failed!", 4);
                Logger.write(ex.Message, 4);
                mw.api_status.Foreground = new SolidColorBrush(Colors.Red);
                return false;
            }

            if (String.IsNullOrEmpty(webhookId))
            {
                Logger.write("Register Device!", 4);
                string prefix = "";
                if (configData.ContainsKey("debug"))
                {
                    prefix = "DEBUG_";
                }

                HomeAssistatnDevice device = new HomeAssistatnDevice
                {
                    device_name = prefix + Environment.MachineName,
                    device_id = (prefix + Environment.MachineName).ToLower(),
                    app_id = Assembly.GetEntryAssembly().GetName().Version.ToString().ToLower(),
                    app_name = Assembly.GetExecutingAssembly().GetName().Name,
                    app_version = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                    manufacturer = Wmic.GetValue("Win32_ComputerSystem", "Manufacturer", "root\\CIMV2"),
                    model = Wmic.GetValue("Win32_ComputerSystem", "Model", "root\\CIMV2"),
                    os_name = Wmic.GetValue("Win32_OperatingSystem", "Caption", "root\\CIMV2"),
                    os_version = Environment.OSVersion.ToString(),
                    app_data = new {
                        push_websocket_channel = true,
                    }
                };
                Logger.write(device);

                Dictionary<string, object> senzorTypes = getSensorsConfiguration();
                device.supports_encryption = false;
                ha.RegisterDevice(device);

                foreach (var item in senzorTypes)
                {
                    string senzorType = item.Key;
                    foreach (var platform in (Dictionary<string, Dictionary<string, List<Dictionary<string, dynamic>>>>)senzorTypes[senzorType])
                    {
                        foreach (var integration in (Dictionary<string, List<Dictionary<string, dynamic>>>)platform.Value)
                        {
                            foreach (var sensorDefinition in (List<Dictionary<string, dynamic>>)integration.Value)
                            {
                                HomeAssistatnSensors senzor = new HomeAssistatnSensors();

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

                                ha.RegisterSensorData(senzor);
                                Thread.Sleep(100);

                                webhookId = ha.getWebhookID();
                                secret = ha.getSecret();
                            }
                        }
                    }
                }

                config.Save(ConfigurationSaveMode.Modified);
            }
            else
            {
                ha.setWebhookID(webhookId);
                ha.setSecret(secret);
                //IpLocation.test();
            }

            if (!sleepRecover)
            {
                update = new DispatcherTimer();
                update.Interval = TimeSpan.FromSeconds(5);
                update.Tick += UpdateSensorTick;
            }
            update.Start();
            Logger.write("Periodic Timer Start!", 0);

            if (configData.ContainsKey("websocket"))
            {
                //WEBSOCKET INITIALIZATION
                ws = new HomeAssistantWS(url.Replace("http", "ws"), webhookId, token);
            }
            return true;
        }

        public void Stop()
        {
            if (update != null)
            {
                update.Stop();
            }
        }

        public void Close(bool sleep = false)
        {
            ws.Close();
            Stop();
            if (!sleep)
            { 
                Environment.Exit(0);
            }
        }

        private async void UpdateSensorTick(object sender, EventArgs e)
        {
            await queryAndSendSenzorData();

            bool lastConnectionStatus = connectionError;
            if (!ha.getConectionStatus() || !ws.getConectionStatus())
            {
                connectionError = true;
                if (lastConnectionStatus != connectionError && connectionError == true)
                {
                    var app = Application.Current as App;
                    app.ShowNotification(Assembly.GetExecutingAssembly().GetName().Name, "Unable to connect to Home Assistant!");
                    ws.Close();
                }
            }
            else
            {
                connectionError = false;
                if (lastConnectionStatus != connectionError && connectionError == false)
                {
                    var app = Application.Current as App;
                    app.ShowNotification(Assembly.GetExecutingAssembly().GetName().Name, "Connection re-established to Home Assistant!");
                    ws.registerAsync();
                }
            }

            mw.api_status.Foreground = (ha.getConectionStatus() ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red));
            mw.ws_status.Foreground = (ws.getConectionStatus() ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red));

            if (configData.ContainsKey("ip_location"))
            {

            }
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
                Logger.write("no senzor scheduled!");
            }
            Logger.write("all task query Done!");

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
                            Logger.write("Filtered Value " + sensorUniqueId + " - " + sensorData);

                            if (string.IsNullOrEmpty(sensorData))
                            {
                                Logger.write("No Data Returned to sensor " + sensorUniqueId);
                                continue;
                            }

                            if (sensorLastValues.ContainsKey(sensorDefinition["unique_id"]))
                            {
                                if (sensorData == sensorLastValues[sensorDefinition["unique_id"]])
                                {
                                    // Logger.write("Skiping! Same Data Already Send " + sensorData);
                                    continue;
                                }
                            }

                            HomeAssistatnSensors senzor = new HomeAssistatnSensors();

                            senzor.unique_id = sensorDefinition["unique_id"];
                            senzor.icon = sensorDefinition["icon"];
                            senzor.state = convertToType(sensorData);
                            senzor.type = senzorType;
                            senzor.unique_id = sensorDefinition["unique_id"];

                            ha.AddSensorData(senzor);

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

            this.ha.sendSensorBuffer();
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

        private static async Task<String> getSenzorValue(KeyValuePair<string, List<Dictionary<string, dynamic>>> integration, Dictionary<string, dynamic> sensorDefinition)
        {
            string className = "HA.Class.Sensors.";

            foreach (var methodNameSegment in integration.Key.Split("_"))
            {
                className += methodNameSegment[0].ToString().ToUpper() + methodNameSegment.Substring(1);
            }

            Type SensorTypeClass = Type.GetType(className);
            if (SensorTypeClass == null)
            {
                Logger.write(className + " Class Not Found");
                throw new Exception(className + " Class Not Found");
            }

            MethodInfo method = SensorTypeClass.GetMethod("GetValue");
            if (method == null)
            {
                Logger.write("GetValue Method Not Found on " + className);
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

        private static Dictionary<string, object> getSensorsConfiguration()
        {
            Dictionary<string, object> senzorTypes = new Dictionary<string, object>();
            senzorTypes.Add("sensor", configData["sensor"]);

            if (configData.ContainsKey("binary_sensor"))
                senzorTypes.Add("binary_sensor", configData["binary_sensor"]);

            return senzorTypes;
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

        public void ShowNotification(string title = "", string body = "", string imageUrl = "", string audioUrl = "", int duration = 5000)
        {
            ToastContentBuilder toast = new ToastContentBuilder();
            toast.AddText(body);

            if (!String.IsNullOrEmpty(title))
            {
                toast.AddText(title);
            }

            if (!String.IsNullOrEmpty(imageUrl))
            {
                string fileName = string.Format("{0}{1}.png", System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
                if (imageUrl.StartsWith("http"))
                {
                    WebClient wc = new WebClient();
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    wc.DownloadFile(imageUrl, fileName);
                 
                    Logger.write("DOWNLOADED");
                }

                Logger.write("file:///" + fileName);
                toast.AddInlineImage(new Uri("file:///" + fileName));
            }

            if (!String.IsNullOrEmpty(audioUrl))
            {
                string fileName = string.Format("{0}{1}.wav", System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
                if (audioUrl.StartsWith("http"))
                {
                    WebClient wc = new WebClient();
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    wc.DownloadFile(audioUrl, fileName);
                }
                Logger.write(fileName);
                
                playNotificationAudio(fileName, duration);

            }
           
            toast.Show();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void keybd_event(uint bVk, uint bScan, uint dwFlags, uint dwExtraInfo);


        public void SendKey(string Key)
        {
            if (!configData.ContainsKey("keys"))
            {
                return;
            }

            try
            {
                uint ukey = (uint)System.Convert.ToUInt32(Key);
                keybd_event(ukey, 0, 0, 0);
                keybd_event(ukey, 0, 2, 0);

            }
            catch (Exception)
            {

                Logger.write("ERROR Type Key");
            }
        }



        private async Task playNotificationAudio(string fileName, int duration)
        {
            System.Media.SoundPlayer player = new System.Media.SoundPlayer();
            player.SoundLocation = fileName;
            player.Play();
            await Task.Delay(duration);
            player.Stop();
        }

        private bool previousProcessDetected()
        {
            Process currentProc = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProc.ProcessName);
            foreach(Process p in processes)
            {
                if ((p.Id != currentProc.Id) && (p.MainModule.FileName == currentProc.MainModule.FileName))
                {
                    return true;
                }
            }
            return false;
        }

        public void minimalizeToTray(bool showNotifycation = true)
        {
            if (showNotifycation)
            {
                ShowNotification("App keeps Running in background!");
            }

            Logger.write("App minimalized");
            MainWindow.ShowInTaskbar = false;
            MainWindow.Hide();
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {

        }

        private void UnhandeledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.write("[" + AppDomain.CurrentDomain.FriendlyName + "]" + e.Exception.Message.ToString(), 3);
            e.Handled = true;
        }
    }

}
