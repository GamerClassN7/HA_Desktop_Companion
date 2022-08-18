
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace HA_Desktop_Companion.Libraries
{
    public class Configuration
    {

        private Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>> configurationData = new Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>>();

        public Configuration(string path)
        {
            string[] text = File.ReadAllLines(path, System.Text.Encoding.UTF8);

            string section = ""; //ROOT
            int num = 0;
            string subSection = "";
            string category = "";

            foreach (string line in text)
            {
                if (line.Contains(':') == true)
                {
                    string parameter = line.Split(':')[0].Trim();
                    string value = line.Split(':')[1].Trim();

                    if (value == "")
                    {
                        section = parameter;
                        num = 0;
                        category = "";
                        subSection = "";
                        continue;
                    }

                    if (parameter.Contains("- ") == true)
                    {
                        if (category == value)
                        {
                            num++;
                        }
                        else
                        {
                            num = 0;
                        }
                        subSection = parameter.Replace("- ", "");
                        category = value;
                        continue;
                    }
                    if (!configurationData.ContainsKey(section))
                    {
                        configurationData[section] = new Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>();
                    }
                    if (!configurationData[section].ContainsKey(subSection))
                    {
                        configurationData[section][subSection] = new Dictionary<string, List<Dictionary<string, string>>>();
                    }
                    if (!configurationData[section][subSection].ContainsKey(category))
                    {
                        configurationData[section][subSection][category] = new List<Dictionary<string, string>>(100);
                    }
                    if (configurationData[section][subSection][category].Count <= num)
                    {
                        configurationData[section][subSection][category].Add(new Dictionary<string, string>());
                    }
                    configurationData[section][subSection][category][num][parameter] = value.Replace("\"", "");
                }
            }


            /*JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };

            MessageBox.Show(JsonSerializer.Serialize(configuration, options));*/
        }

        public Dictionary<string, Dictionary<string, Dictionary<string, List<Dictionary<string, string>>>>> GetConfigurationData()
        {
            return configurationData;
        }
    }
}
