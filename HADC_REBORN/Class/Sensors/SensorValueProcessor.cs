using HADC_REBORN.Class.Config;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HADC_REBORN.Class.Sensors
{
    // Turns the raw value read by a sensor platform into the state sent to Home Assistant
    public static class SensorValueProcessor
    {
        public static string Process(SensorConfig sensor, string sensorData, Action<string>? log = null)
        {
            if (sensor.Type == SensorConfig.TypeBinarySensor)
            {
                return sensorData;
            }

            if (string.IsNullOrEmpty(sensorData))
            {
                sensorData = "0";
            }

            if (sensor.ValueMap != null)
            {
                if (Int32.TryParse(sensorData, out int valueMapIndex) && valueMapIndex >= 0 && valueMapIndex < sensor.ValueMap.Length)
                {
                    sensorData = sensor.ValueMap[valueMapIndex];
                }
                else
                {
                    log?.Invoke("Value '" + sensorData + "' out of value_map range for " + sensor.UniqueId);
                }
            }

            if (sensor.Filters.Count > 0 && TryParseNumber(sensorData, out double filteredValue))
            {
                foreach (SensorFilter filter in sensor.Filters)
                {
                    filteredValue = filter.Apply(filteredValue);
                }

                sensorData = filteredValue.ToString(CultureInfo.InvariantCulture);
            }

            if (sensor.AccuracyDecimals != null && TryParseNumber(sensorData, out double roundedValue))
            {
                sensorData = Math.Round(roundedValue, Math.Clamp(sensor.AccuracyDecimals.Value, 0, 15)).ToString(CultureInfo.InvariantCulture);
            }

            return sensorData;
        }

        // Values are always formatted with InvariantCulture, accept a decimal comma too (e.g. Czech locale)
        public static bool TryParseNumber(string value, out double number)
        {
            number = 0;
            if (string.IsNullOrEmpty(value) || !Regex.IsMatch(value, @"^-?\d+([.,]\d+)?$"))
            {
                return false;
            }

            return double.TryParse(value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }

        // State as JSON type: bool, number or string
        public static object ConvertToType(string value)
        {
            if (Regex.IsMatch(value, "^(?:tru|fals)e$", RegexOptions.IgnoreCase))
            {
                return bool.Parse(value);
            }

            if (TryParseNumber(value, out double number))
            {
                return number;
            }

            return value;
        }
    }
}
