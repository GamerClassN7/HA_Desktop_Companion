
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text.Json;
using System.Windows;

namespace HA_Desktop_Companion.Libraries
{
    public class Configuration
    {
        private string configPath;
        private Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, dynamic>>>>> configurationData = new Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, dynamic>>>>>();

        public Configuration(string path)
        {
            configPath = path;
            if (!File.Exists(configPath))
            {
                //Try to download latest from github if fail use resources
                //Write internall backup if file did not exist 
                File.WriteAllBytes(configPath, Resource1.configuration);
                
                do
                {
                    System.Threading.Thread.Sleep(1);
                } while (!File.Exists(configPath));
            }
        }

        public bool load(){
            if (!File.Exists(configPath))
            {
                return false;
            }

            string[] text = File.ReadAllLines(configPath, System.Text.Encoding.UTF8);

            string section = ""; //ROOT
            int num = 0;
            string subSection = "";
            string category = "";
            string subArray = "";

            foreach (string line in text)
            {
                 string localLine = line;
                if (line.Contains('#') == true)
                {
                    localLine = line.Replace('#' + line.Split('#')[1], "");
                }
                if (localLine.Contains(':') == true)
                {
                    string[] splitedString = localLine.Split(':');
                    string parameter = splitedString[0].Trim();
                    string value = splitedString[1].Trim();

                    if (parameter == "")
                    {
                        continue;
                    }

                    if (subArray != "" && !splitedString[0].Contains("     "))
                    {
                        subArray = "";
                    }

                    if (splitedString.Length > 2)
                    {
                        for (int i = 2; i < splitedString.Length; i++)
                        {
                            value += ":" + splitedString[i].Trim();
                        }
                    }

                    if (value == "")
                    {
                        if (splitedString[0].Contains("    "))
                        {
                            subArray = parameter;
                            continue;
                        }
                        else
                        {
                            subArray = "";
                        }
                        if (section != "")
                        {
                            if (!configurationData.ContainsKey(section))
                            {
                                configurationData[section] = null;
                            }
                        }
                        section = parameter;
                        num = 0;
                        category = "";
                        subSection = "";
                        continue;
                    }

                    if (parameter.Contains("- ") == true)
                    {
                        if (subArray == "") {
                            subSection = parameter.Replace("- ", "");

                            if (category == value)
                            {
                                num++;
                            }
                            else
                            {
                                if (!configurationData.ContainsKey(section) || !configurationData[section].ContainsKey(subSection) || !configurationData[section][subSection].ContainsKey(value) || configurationData[section][subSection][value].Count <= 0)
                                {
                                    num = 0;
                                }
                                else
                                {
                                    num = configurationData[section][subSection][value].Count;
                                }
                            }

                            category = value;
                            continue;
                        }
                        else
                        {
                            parameter = parameter.Replace("- ", "");
                        }
                    }


                    if (!configurationData.ContainsKey(section))
                    {
                        configurationData[section] = new Dictionary<string, Dictionary<string, List<Dictionary<string, dynamic>>>>();
                    }
                    if (!configurationData[section].ContainsKey(subSection))
                    {
                        configurationData[section][subSection] = new Dictionary<string, List<Dictionary<string, dynamic>>>();
                    }
                    if (!configurationData[section][subSection].ContainsKey(category))
                    {
                        configurationData[section][subSection][category] = new List<Dictionary<string, dynamic>>(100);
                    }
                    if (configurationData[section][subSection][category].Count <= num)
                    {
                        configurationData[section][subSection][category].Add(new Dictionary<string, dynamic>());
                    }
                    if (subArray == "")
                    {
                        configurationData[section][subSection][category][num][parameter] = value.Replace("\"", "");
                    }
                    else
                    {
                        if (!configurationData[section][subSection][category][num].ContainsKey(subArray))
                        {
                            configurationData[section][subSection][category][num][subArray] = new Dictionary<string, string>();
                        }
                        configurationData[section][subSection][category][num][subArray][parameter] = value.Replace("\"", "");
                    }
                }
            }

            return true;
        }

        public Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, dynamic>>>>> GetConfigurationData()
        {
            //MessageBox.Show(JsonSerializer.Serialize(configurationData));
            return configurationData;
        }
    }
}
