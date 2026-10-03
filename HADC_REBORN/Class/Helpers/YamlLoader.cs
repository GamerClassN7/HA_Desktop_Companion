using HADC_REBORN.Class.Config;
using HADC_REBORN.Class.Sensors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace HADC_REBORN.Class.Helpers
{
    public class YamlConfigurationException : Exception
    {
        public YamlConfigurationException(string message, Exception? innerException = null) : base(message, innerException) { }
    }

    public class YamlLoader
    {
        private static readonly string[] filterOperations = { "multiply", "divide", "deduct", "add" };

        private AppConfiguration configurationData;

        // Problems that don't prevent the app from running (e.g. duplicate unique_id)
        public List<string> Warnings { get; } = new List<string>();

        // Invalid sensor definitions, these sensors are skipped
        public List<string> Errors { get; } = new List<string>();

        public YamlLoader(string configurationFileFullPath)
        {
            if (!File.Exists(configurationFileFullPath))
            {
                throw new YamlConfigurationException("'configuration.yaml' not found");
            }

            string yaml = File.ReadAllText(configurationFileFullPath, System.Text.Encoding.UTF8);
            configurationData = Parse(yaml, platform => SensorPlatforms.TryGet(platform, out ISensorPlatform sensorPlatform) ? sensorPlatform.RequiredParameters : null, Warnings, Errors);
        }

        public AppConfiguration getConfigurationData()
        {
            return configurationData;
        }

        // requiredParametersOfPlatform returns null for an unknown platform
        // Invalid YAML throws, an invalid sensor is only reported in errors and skipped so the other sensors keep working
        public static AppConfiguration Parse(string yaml, Func<string, string[]?> requiredParametersOfPlatform, List<string>? warnings = null, List<string>? errors = null)
        {
            warnings ??= new List<string>();
            errors ??= new List<string>();

            YamlStream stream = new YamlStream();
            try
            {
                stream.Load(new StringReader(yaml.TrimStart('\uFEFF')));
            }
            catch (YamlException e)
            {
                throw new YamlConfigurationException("configuration.yaml line " + e.Start.Line + ": " + (e.InnerException?.Message ?? e.Message), e);
            }

            AppConfiguration configuration = new AppConfiguration();
            if (stream.Documents.Count == 0 || stream.Documents[0].RootNode is YamlScalarNode { Value: null or "" })
            {
                return configuration;
            }

            if (stream.Documents[0].RootNode is not YamlMappingNode root)
            {
                throw error(stream.Documents[0].RootNode, "expected 'key: value' pairs at the top level");
            }

            foreach (KeyValuePair<YamlNode, YamlNode> entry in root.Children)
            {
                string key = scalar(entry.Key, "key");
                switch (key)
                {
                    case "websocket":
                        configuration.Websocket = parseBool(entry.Value, key);
                        break;
                    case "debug":
                        configuration.Debug = parseBool(entry.Value, key);
                        break;
                    case "ip_location":
                        configuration.IpLocation = parseBool(entry.Value, key);
                        break;
                    case "keys":
                        configuration.Keys = parseBool(entry.Value, key);
                        break;
                    case SensorConfig.TypeSensor:
                        configuration.Sensors = parseSensors(entry.Value, SensorConfig.TypeSensor, requiredParametersOfPlatform, errors);
                        break;
                    case SensorConfig.TypeBinarySensor:
                        configuration.BinarySensors = parseSensors(entry.Value, SensorConfig.TypeBinarySensor, requiredParametersOfPlatform, errors);
                        break;
                    default:
                        warnings.Add("line " + entry.Key.Start.Line + ": unknown option '" + key + "' ignored");
                        break;
                }
            }

            // Keep the first sensor with a given unique_id, same as before
            HashSet<string> uniqueIds = new HashSet<string>();
            foreach (SensorConfig sensor in configuration.AllSensors.ToList())
            {
                if (!uniqueIds.Add(sensor.UniqueId))
                {
                    warnings.Add("duplicate unique_id '" + sensor.UniqueId + "', only the first definition is used");
                    configuration.Sensors.Remove(sensor);
                    configuration.BinarySensors.Remove(sensor);
                }
            }

            return configuration;
        }

        private static List<SensorConfig> parseSensors(YamlNode node, string type, Func<string, string[]?> requiredParametersOfPlatform, List<string> errors)
        {
            List<SensorConfig> sensors = new List<SensorConfig>();
            if (isEmpty(node))
            {
                return sensors;
            }

            if (node is not YamlSequenceNode sequence)
            {
                throw error(node, "'" + type + "' must be a list of sensors (lines starting with '- platform: ...')");
            }

            foreach (YamlNode item in sequence.Children)
            {
                try
                {
                    if (item is not YamlMappingNode mapping)
                    {
                        throw error(item, "each " + type + " must be a set of 'key: value' options");
                    }

                    sensors.Add(parseSensor(mapping, type, requiredParametersOfPlatform));
                }
                catch (YamlConfigurationException e)
                {
                    errors.Add(e.Message);
                }
            }

            return sensors;
        }

        private static SensorConfig parseSensor(YamlMappingNode mapping, string type, Func<string, string[]?> requiredParametersOfPlatform)
        {
            SensorConfig sensor = new SensorConfig { Type = type };

            foreach (KeyValuePair<YamlNode, YamlNode> entry in mapping.Children)
            {
                string key = scalar(entry.Key, "key");
                switch (key)
                {
                    case "platform":
                        sensor.Platform = scalar(entry.Value, key);
                        break;
                    case "name":
                        sensor.Name = scalar(entry.Value, key);
                        break;
                    case "unique_id":
                        sensor.UniqueId = scalar(entry.Value, key);
                        break;
                    case "icon":
                        sensor.Icon = scalar(entry.Value, key);
                        break;
                    case "device_class":
                        sensor.DeviceClass = scalar(entry.Value, key);
                        break;
                    case "unit_of_measurement":
                        sensor.UnitOfMeasurement = scalar(entry.Value, key);
                        break;
                    case "state_class":
                        sensor.StateClass = scalar(entry.Value, key);
                        break;
                    case "entity_category":
                        sensor.EntityCategory = scalar(entry.Value, key);
                        break;
                    case "disabled":
                        sensor.Disabled = parseBool(entry.Value, key);
                        break;
                    case "update_interval":
                        sensor.UpdateInterval = parseDouble(entry.Value, key);
                        break;
                    case "accuracy_decimals":
                        sensor.AccuracyDecimals = (int)parseDouble(entry.Value, key);
                        break;
                    case "value_map":
                        sensor.ValueMap = entry.Value is YamlSequenceNode values
                            ? values.Children.Select(x => scalar(x, key)).ToArray()
                            : scalar(entry.Value, key).Split('|');
                        break;
                    case "filters":
                        sensor.Filters = parseFilters(entry.Value);
                        break;
                    default:
                        if (entry.Value is not YamlScalarNode)
                        {
                            throw error(entry.Value, "option '" + key + "' must be a single value");
                        }
                        sensor.Parameters[key] = scalar(entry.Value, key);
                        break;
                }
            }

            if (String.IsNullOrEmpty(sensor.Platform))
            {
                throw error(mapping, type + " is missing 'platform'");
            }

            if (String.IsNullOrEmpty(sensor.UniqueId))
            {
                throw error(mapping, type + " with platform '" + sensor.Platform + "' is missing 'unique_id'");
            }

            if (String.IsNullOrEmpty(sensor.Name))
            {
                throw error(mapping, type + " '" + sensor.UniqueId + "' is missing 'name'");
            }

            string[]? requiredParameters = requiredParametersOfPlatform(sensor.Platform);
            if (requiredParameters == null)
            {
                throw error(mapping, type + " '" + sensor.UniqueId + "' has unknown platform '" + sensor.Platform + "'");
            }

            string[] missing = requiredParameters.Where(x => !sensor.HasParameter(x)).ToArray();
            if (missing.Length > 0)
            {
                throw error(mapping, type + " '" + sensor.UniqueId + "' (platform " + sensor.Platform + ") is missing '" + String.Join("', '", missing) + "'");
            }

            return sensor;
        }

        // Accepts both a list ("- divide: 1024") and a map ("divide: 1024"), lists are applied in the given order
        private static List<SensorFilter> parseFilters(YamlNode node)
        {
            List<SensorFilter> filters = new List<SensorFilter>();
            if (isEmpty(node))
            {
                return filters;
            }

            IEnumerable<KeyValuePair<YamlNode, YamlNode>> entries;
            if (node is YamlSequenceNode sequence)
            {
                entries = sequence.Children.SelectMany(item => item is YamlMappingNode filterMapping
                    ? filterMapping.Children
                    : throw error(item, "each filter must be like '- divide: 1024'"));
            }
            else if (node is YamlMappingNode mapping)
            {
                entries = mapping.Children;
            }
            else
            {
                throw error(node, "'filters' must be a list like '- divide: 1024'");
            }

            foreach (KeyValuePair<YamlNode, YamlNode> entry in entries)
            {
                string operation = scalar(entry.Key, "filter");
                if (!filterOperations.Contains(operation))
                {
                    throw error(entry.Key, "unknown filter '" + operation + "', supported: " + String.Join(", ", filterOperations));
                }

                double value = parseDouble(entry.Value, operation);
                if (operation == "divide" && value == 0)
                {
                    throw error(entry.Value, "filter 'divide' can't be 0");
                }

                filters.Add(new SensorFilter { Operation = operation, Value = value });
            }

            return filters;
        }

        private static bool isEmpty(YamlNode node)
        {
            return node is YamlScalarNode scalarNode && String.IsNullOrEmpty(scalarNode.Value);
        }

        private static string scalar(YamlNode node, string key)
        {
            if (node is not YamlScalarNode scalarNode)
            {
                throw error(node, "'" + key + "' must be a single value");
            }

            return scalarNode.Value ?? "";
        }

        private static bool parseBool(YamlNode node, string key)
        {
            string value = scalar(node, key).Trim().ToLowerInvariant();
            switch (value)
            {
                // "keys:" without a value used to enable the option
                case "":
                case "true":
                case "yes":
                case "on":
                case "1":
                    return true;
                case "false":
                case "no":
                case "off":
                case "0":
                    return false;
                default:
                    throw error(node, "'" + key + "' must be true or false");
            }
        }

        private static double parseDouble(YamlNode node, string key)
        {
            string value = scalar(node, key);
            if (!double.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                throw error(node, "'" + key + "' must be a number");
            }

            return result;
        }

        private static YamlConfigurationException error(YamlNode node, string message)
        {
            return new YamlConfigurationException("configuration.yaml line " + node.Start.Line + ": " + message);
        }
    }
}
