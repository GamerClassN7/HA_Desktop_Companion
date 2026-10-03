using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HADC_REBORN.Class.Config
{
    // Typed representation of configuration.yaml
    public class AppConfiguration
    {
        public bool Websocket { get; set; } = true;
        public bool Debug { get; set; } = false;
        public bool IpLocation { get; set; } = false;
        public bool Keys { get; set; } = false;

        public List<SensorConfig> Sensors { get; set; } = new List<SensorConfig>();
        public List<SensorConfig> BinarySensors { get; set; } = new List<SensorConfig>();

        public IEnumerable<SensorConfig> AllSensors => Sensors.Concat(BinarySensors);
    }

    public class SensorConfig
    {
        public const string TypeSensor = "sensor";
        public const string TypeBinarySensor = "binary_sensor";

        // "sensor" or "binary_sensor"
        public string Type { get; set; } = TypeSensor;

        public string Platform { get; set; } = "";
        public string Name { get; set; } = "";
        public string UniqueId { get; set; } = "";
        public string? Icon { get; set; }
        public string? DeviceClass { get; set; }
        public string? UnitOfMeasurement { get; set; }
        public string? StateClass { get; set; }
        public string? EntityCategory { get; set; }
        public bool? Disabled { get; set; }

        // Seconds between updates, null = every update cycle
        public double? UpdateInterval { get; set; }
        public int? AccuracyDecimals { get; set; }
        public string[]? ValueMap { get; set; }
        public List<SensorFilter> Filters { get; set; } = new List<SensorFilter>();

        // Platform specific options (wmic_class, selector, consent_category, ...)
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool HasParameter(string key)
        {
            return Parameters.ContainsKey(key) && !String.IsNullOrEmpty(Parameters[key]);
        }

        public string GetParameter(string key, string defaultValue = "")
        {
            return Parameters.TryGetValue(key, out string? value) ? value : defaultValue;
        }

        public int GetIntParameter(string key, int defaultValue = 0)
        {
            return Parameters.TryGetValue(key, out string? value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : defaultValue;
        }
    }

    public class SensorFilter
    {
        public string Operation { get; set; } = "";
        public double Value { get; set; }

        public double Apply(double input)
        {
            switch (Operation)
            {
                case "multiply": return input * Value;
                case "divide": return input / Value;
                case "deduct": return input - Value;
                case "add": return input + Value;
                default: throw new InvalidOperationException("Unknown filter '" + Operation + "'");
            }
        }
    }
}
