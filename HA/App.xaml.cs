using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HA.Class.HomeAssistant;
using HA.Class.HomeAssistant.Objects;
using HA.Class.Sensors;
using HA.Class.YamlConfiguration;

namespace HA
{
    /// <summary>
    /// Interaction logic for App.xaml test 
    /// </summary>
    public partial class App : Application
    {
        static HomeAssistantAPI ha;
        static DispatcherTimer? update = null;
        static Dictionary<string, DateTime> sensorUpdatedAtList = new Dictionary<string, DateTime>();
        static Dictionary<string, string> sensorLastValues = new Dictionary<string, string>();


        private static string appDir = Directory.GetCurrentDirectory();
        private static YamlConfiguration configurationObject = new YamlConfiguration(appDir + "/configuration.yaml");
        private static Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, dynamic>>>>> configData;

        void App_Startup(object sender, StartupEventArgs e)
        {
           
        }

        public static bool Start()
        {
            //Clear check Buffers
            sensorLastValues.Clear();
            sensorUpdatedAtList.Clear();

            //Load Config
            if (!configurationObject.LoadConfiguration())
            {
                MessageBox.Show("Config Error Report to Developer!");
            }
            configData = configurationObject.GetConfigurationData();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            string token = config.AppSettings.Settings["token"].Value;
            string url = config.AppSettings.Settings["url"].Value;
            string webhookId = config.AppSettings.Settings["webhookId"].Value;
            string secret = config.AppSettings.Settings["secret"].Value;

            try
            {
                ha = new HomeAssistantAPI(url, token);
            } catch {
                return false;
            }

            try
            {
                ha.GetVersion();
            }
            catch
            {
                return false;
            }

            if (String.IsNullOrEmpty(webhookId))
            {
                HomeAssistatnDevice device = new HomeAssistatnDevice();
                device.device_name = "DEBUG_" + System.Environment.MachineName;
                device.device_id = "DEBUG_" + System.Environment.MachineName;
                device.app_id = Assembly.GetEntryAssembly().GetName().Version.ToString().ToLower();
                device.app_name = Assembly.GetExecutingAssembly().GetName().Name;
                device.app_version = Assembly.GetEntryAssembly().GetName().Version.ToString();
                device.manufacturer = Wmic.GetValue("Win32_ComputerSystem", "Manufacturer", "root\\CIMV2");
                device.model = Wmic.GetValue("Win32_ComputerSystem", "Model", "root\\CIMV2");
                device.os_name = Wmic.GetValue("Win32_OperatingSystem", "Caption", "root\\CIMV2");
                device.os_version = Environment.OSVersion.ToString(); ;
                device.supports_encryption = false;
                MessageBox.Show(ha.RegisterDevice(device));

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
                            }
                        }
                    }
                }
            }
            else
            {
                ha.setWebhookID(webhookId);
                ha.setSecret(secret);
            }

            update = new DispatcherTimer();
            update.Interval = TimeSpan.FromSeconds(5);
            update.Tick += UpdateSenzorTick;
            update.Start();

            return true;
        }

        public static void Stop()
        {
            if (update != null)
            {
                update.Stop();
            }
        }

        static async void UpdateSenzorTick(object sender, EventArgs e)
        {
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
                            if (sensorUpdatedAtList.ContainsKey(sensorDefinition["unique_id"]) && sensorDefinition.ContainsKey("update_interval"))
                            {
                                TimeSpan difference = DateTime.Now.Subtract(sensorUpdatedAtList[sensorDefinition["unique_id"]]);
                                if (difference.TotalSeconds < Double.Parse(sensorDefinition["update_interval"])) {
                                    continue;
                                }
                            }

                            string sensorUniqueId = sensorDefinition["unique_id"];
                            string className = "HA.Class.Sensors.";

                            foreach (var methodNameSegment in integration.Key.Split("_"))
                            {
                                className += methodNameSegment[0].ToString().ToUpper() + methodNameSegment.Substring(1);
                            }

                            Debug.WriteLine(className);

                            Type SensorTypeClass = Type.GetType(className);
                            if (SensorTypeClass == null)
                            {
                                Debug.WriteLine(className + " Class Not Found");
                                continue;
                            }

                            MethodInfo method = SensorTypeClass.GetMethod("GetValue");
                            if (method == null)
                            {
                                Debug.WriteLine("Method Not Found on " + className);
                                continue;
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

                            string sensorData = method.Invoke(null, parameters.ToArray()).ToString();

                            if (senzorType != "binary_sensor")
                            {
                                if (sensorDefinition.ContainsKey("value_map"))
                                {
                                    string[] valueMap = sensorDefinition["value_map"].Split("|");
                                    sensorData = valueMap[(Int32.Parse((sensorData).ToString()))];
                                }

                                if (sensorDefinition.ContainsKey("filters"))
                                {
                                    bool isNumeric = int.TryParse(sensorData, out _);
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
                            }

                            if (string.IsNullOrEmpty(sensorData))
                            {
                                Debug.WriteLine("No Data Returned to sensor " + sensorUniqueId);
                                continue;
                            }

                            if (sensorLastValues.ContainsKey(sensorDefinition["unique_id"]))
                            {
                                if (sensorData == sensorLastValues[sensorDefinition["unique_id"]])
                                {
                                    Debug.WriteLine("Skiping! Same Data Already Send " + sensorData);
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

                            } else {
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

            ha.sendSensorBuffer();
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
            //Debug.WriteLine("BEFORE CONVERSION" + variableStr);
            if (Regex.IsMatch(variableStr, "^(?:tru|fals)e$", RegexOptions.IgnoreCase))
            {
                //Debug.WriteLine("AFTER CONVERSION (Bool)" + variableStr.ToString());
                return bool.Parse(variableStr);
            }
            else if (Regex.IsMatch(variableStr, @"^[0-9]+.[0-9]+$"))
            {
                //Debug.WriteLine("AFTER CONVERSION (double)" + variableStr.ToString());
                return double.Parse(variableStr);
            }
            else if (Regex.IsMatch(variableStr, @"^\d$"))
            {
                //Debug.WriteLine("AFTER CONVERSION (int)" + variableStr.ToString());
                return int.Parse(variableStr);
            }

            //Debug.WriteLine("AFTER CONVERSION" + variableStr.ToString());
            return variableStr;
        }
    }
}
