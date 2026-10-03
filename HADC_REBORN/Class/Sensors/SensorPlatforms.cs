using HADC_REBORN.Class.Config;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HADC_REBORN.Class.Sensors
{
    public interface ISensorPlatform
    {
        string Name { get; }

        // Options that must be present in the sensor definition, checked when configuration.yaml is loaded
        string[] RequiredParameters { get; }

        string GetValue(SensorConfig sensor);
    }

    internal class SensorPlatform : ISensorPlatform
    {
        private readonly Func<SensorConfig, object?> query;

        public string Name { get; }
        public string[] RequiredParameters { get; }

        public SensorPlatform(string name, string[] requiredParameters, Func<SensorConfig, object?> query)
        {
            Name = name;
            RequiredParameters = requiredParameters;
            this.query = query;
        }

        public string GetValue(SensorConfig sensor)
        {
            return Convert.ToString(query(sensor), CultureInfo.InvariantCulture) ?? "";
        }
    }

    // Maps the "platform" from configuration.yaml to the code reading the value
    public static class SensorPlatforms
    {
        private static readonly Dictionary<string, ISensorPlatform> platforms = new Dictionary<string, ISensorPlatform>(StringComparer.OrdinalIgnoreCase);

        static SensorPlatforms()
        {
            register(new SensorPlatform("wmic", new[] { "wmic_class", "wmic_selector" }, s => Wmic.GetValue(
                s.GetParameter("wmic_class"),
                s.GetParameter("wmic_selector"),
                s.GetParameter("wmic_namespace", @"root\wmi"),
                s.GetIntParameter("wmic_iterator_index", 0))));
            register(new SensorPlatform("network_interface", new[] { "selector" }, s => NetworkInterface.GetValue(s.GetParameter("selector"), s.GetParameter("deselector"))));
            // Name used by configuration.yaml of the original HA app
            register(new SensorPlatform("wifi", new[] { "selector" }, s => NetworkInterface.GetValue(s.GetParameter("selector"), s.GetParameter("deselector"))));
            register(new SensorPlatform("consent_store", new[] { "consent_category" }, s => ConsentStore.GetValue(s.GetParameter("consent_category"))));
            register(new SensorPlatform("ping", new[] { "host" }, s => Ping.GetValue(s.GetParameter("host"))));
            register(new SensorPlatform("current_window", new string[0], s => CurrentWindow.GetValue()));
            register(new SensorPlatform("uptime", new string[0], s => Uptime.GetValue()));
            register(new SensorPlatform("restart_pending", new string[0], s => RestartPending.GetValue()));
            register(new SensorPlatform("locked_state", new string[0], s => LockedState.GetValue()));
        }

        private static void register(ISensorPlatform platform)
        {
            platforms[platform.Name] = platform;
        }

        public static IEnumerable<string> Names => platforms.Keys.OrderBy(x => x);

        public static bool TryGet(string name, out ISensorPlatform platform)
        {
            return platforms.TryGetValue(name, out platform!);
        }
    }
}
