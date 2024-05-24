

using HADC_REBORN.Class.Helpers;

YamlLoader yaml = new YamlLoader("./configuration.yaml");




// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(yaml.getConfigurationData()));
