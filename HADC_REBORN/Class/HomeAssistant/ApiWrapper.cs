using HADC_REBORN.Class.Helpers;
using HADC_REBORN.Class.HomeAssistant.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.HomeAssistant
{
    public class ApiWrapper
    {
        static Dictionary<string, DateTime> sensorUpdatedAtList = new Dictionary<string, DateTime>();
        static Dictionary<string, dynamic> sensorLastValues = new Dictionary<string, dynamic>();
        //static Dictionary<string, bool> sensorFailed= new Dictionary<string, bool>();

        private YamlLoader yamlLoader;
        private ApiConnector apiConnector;

        public ApiWrapper(YamlLoader yamlLoaderDependency, ApiConnector apiConnectorDependency) {
            yamlLoader = yamlLoaderDependency;
            apiConnector = apiConnectorDependency;
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

        public async Task queryAndSendSenzorData()
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
                                    App.log.writeLine("Skiping: " + sensorUniqueId + " sensor Update time not Reached");
                                    continue;
                                }
                            }

                            //if (sensorFailed.ContainsKey(sensorUniqueId) && sensorFailed[sensorUniqueId] != false)
                            //{
                            //   App.log.writeLine("Skiping previouselly failed: " + sensorUniqueId + " sensor");
                            //   continue;
                            //}

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

                            apiConnector.AddSensorData(senzor);

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

            apiConnector.sendSensorBuffer();
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

        public Dictionary<string, object> getSensorsConfiguration()
        {
            Dictionary<string, object> senzorTypes = new Dictionary<string, object>();
            senzorTypes.Add("sensor", yamlLoader.getConfigurationData()["sensor"]);

            if (yamlLoader.getConfigurationData().ContainsKey("binary_sensor"))
                senzorTypes.Add("binary_sensor", yamlLoader.getConfigurationData()["binary_sensor"]);

            return senzorTypes;
        }

        private static Task<string> getSenzorValue(KeyValuePair<string, List<Dictionary<string, dynamic>>> integration, Dictionary<string, dynamic> sensorDefinition)
        {
            string className = "HADC_REBORN.Class.Sensors.";
            string sensorUniqueId = sensorDefinition["unique_id"];

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
                if (p == null)
                {
                    continue;
                }

                if (p.Name != null && sensorDefinition.ContainsKey(p.Name))
                {
                    parameters.Insert(p.Position, sensorDefinition[p.Name]);
                }
                else if (p.IsOptional && p.DefaultValue != null)
                {
                    parameters.Insert(p.Position, p.DefaultValue);
                }
            }

            return Task.Run<string>(() => {
                return method.Invoke(null, parameters.ToArray()).ToString(); 
            });
        }
    }
}
