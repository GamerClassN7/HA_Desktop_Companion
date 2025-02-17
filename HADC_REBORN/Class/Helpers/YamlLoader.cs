using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace HADC_REBORN.Class.Helpers
{
    public class YamlLoader
    {
        private string configurationFilePath;
        private Dictionary<string, dynamic> configurationData;

        public YamlLoader(string configurationFileFullPath)
        {
            if (!File.Exists(configurationFileFullPath))
            {
                throw new Exception("'configuration.yaml' not found");
            }

            configurationFilePath = configurationFileFullPath;
            load();
        }

        private bool load()
        {
            try
            {
                string[] text = File.ReadAllLines(configurationFilePath, System.Text.Encoding.UTF8);

                string section = ""; //ROOT
                int num = 0;
                string subSection = "";
                string category = "";
                string subArray = "";
                Dictionary<string, dynamic> configurationDataTemp = new Dictionary<string, dynamic>();

                foreach (string line in text)
                {
                    string localLine = line;

                    if (line.StartsWith('#'))
                    {
                        continue;
                    }

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
                            subArray = "";

                            if (section != "")
                            {
                                if (!configurationDataTemp.ContainsKey(section))
                                {
                                    configurationDataTemp[section] = null;
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
                            if (subArray == "")
                            {
                                subSection = parameter.Replace("- ", "");

                                if (category == value)
                                {
                                    num++;
                                }
                                else
                                {
                                    if (!configurationDataTemp.ContainsKey(section) || !configurationDataTemp[section].ContainsKey(subSection) || !configurationDataTemp[section][subSection].ContainsKey(value) || configurationDataTemp[section][subSection][value].Count <= 0)
                                    {
                                        num = 0;
                                    }
                                    else
                                    {
                                        num = configurationDataTemp[section][subSection][value].Count;
                                    }
                                }

                                category = value;
                                continue;
                            }
                            parameter = parameter.Replace("- ", "");
                        }

                        if (String.IsNullOrEmpty(section))
                        {
                            configurationDataTemp[parameter] = value.Replace("\"", "");
                            continue;
                        }

                        if (!configurationDataTemp.ContainsKey(section))
                        {
                            configurationDataTemp[section] = new Dictionary<string, dynamic>();
                        }

                        if (!configurationDataTemp[section].ContainsKey(subSection))
                        {
                            configurationDataTemp[section][subSection] = new Dictionary<string, List<Dictionary<string, dynamic>>>();
                        }

                        if (!configurationDataTemp[section][subSection].ContainsKey(category))
                        {
                            configurationDataTemp[section][subSection][category] = new List<Dictionary<string, dynamic>>(100);
                        }

                        if (configurationDataTemp[section][subSection][category].Count <= num)
                        {
                            configurationDataTemp[section][subSection][category].Add(new Dictionary<string, dynamic>());
                        }

                        if (subArray == "")
                        {
                            configurationDataTemp[section][subSection][category][num][parameter] = value.Replace("\"", "");
                        }
                        else
                        {
                            if (!configurationDataTemp[section][subSection][category][num].ContainsKey(subArray))
                            {
                                configurationDataTemp[section][subSection][category][num][subArray] = new Dictionary<string, string>();
                            }
                            configurationDataTemp[section][subSection][category][num][subArray][parameter] = value.Replace("\"", "");
                        }
                    }
                }

                configurationData = configurationDataTemp;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Dictionary<string, dynamic> getConfigurationData()
        {
            return configurationData;
        }
    }
}
