using HADC_REBORN.Class.Config;
using HADC_REBORN.Class.Helpers;
using HADC_REBORN.Class.HomeAssistant.Objects;
using HADC_REBORN.Class.Sensors;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace HADC_REBORN.Class.HomeAssistant
{
    public class ApiWrapper
    {
        private readonly Dictionary<string, DateTime> sensorUpdatedAtList = new Dictionary<string, DateTime>();
        private readonly Dictionary<string, string> sensorLastValues = new Dictionary<string, string>();
        // Registration (connect / re-registration) and the update worker run on different threads
        private readonly object stateLock = new object();

        private AppConfiguration configuration;
        private ApiConnector apiConnector;
        private Configuration config;

        private BackgroundWorker apiWorker = new BackgroundWorker();

        private DispatcherTimer apiTimer = new DispatcherTimer();

        // Raised with the new webhook_id after the device had to be registered again
        public event Action<string>? WebhookChanged;

        public ApiWrapper(AppConfiguration configurationDependency, ApiConnector apiConnectorDependency, Configuration configDependency) {
            configuration = configurationDependency;
            apiConnector = apiConnectorDependency;
            config = configDependency;

            // Handlers are attached only once, otherwise every restart would multiply them
            apiWorker.DoWork += apiWorker_DoWork;

            apiTimer.Interval = TimeSpan.FromSeconds(5);
            apiTimer.Tick += updateSensors;
        }

        public async Task queryAndSendSenzorData()
        {
            DateTime now = DateTime.Now;
            List<SensorConfig> dueSensors;
            lock (stateLock)
            {
                dueSensors = configuration.AllSensors.Where(sensor =>
                    sensor.UpdateInterval == null
                    || !sensorUpdatedAtList.ContainsKey(sensor.UniqueId)
                    || (now - sensorUpdatedAtList[sensor.UniqueId]).TotalSeconds >= sensor.UpdateInterval
                ).ToList();
            }

            if (dueSensors.Count < 1)
            {
                return;
            }

            Dictionary<SensorConfig, Task<string>> senzorsQuerys = new Dictionary<SensorConfig, Task<string>>();
            foreach (SensorConfig sensor in dueSensors)
            {
                if (!SensorPlatforms.TryGet(sensor.Platform, out ISensorPlatform platform))
                {
                    App.log.writeLine("Unknown platform '" + sensor.Platform + "' of sensor " + sensor.UniqueId);
                    continue;
                }

                senzorsQuerys.Add(sensor, Task.Run(() => platform.GetValue(sensor)));
            }

            try
            {
                await Task.WhenAll(senzorsQuerys.Values.ToArray());
            }
            catch (Exception)
            {
                // Failed sensors are logged and skipped individually below, so the rest still get sent
            }

            lock (stateLock)
            {
                foreach (KeyValuePair<SensorConfig, Task<string>> query in senzorsQuerys)
                {
                    SensorConfig sensor = query.Key;
                    if (!query.Value.IsCompletedSuccessfully)
                    {
                        App.log.writeLine("Failed to query sensor " + sensor.UniqueId + ": " + query.Value.Exception?.GetBaseException().Message);
                        continue;
                    }

                    string sensorData;
                    try
                    {
                        sensorData = SensorValueProcessor.Process(sensor, query.Value.Result, message => App.log.writeLine(message));
                    }
                    catch (Exception ex)
                    {
                        App.log.writeLine("Failed to apply filters to sensor " + sensor.UniqueId + ": " + ex.Message);
                        continue;
                    }

                    if (string.IsNullOrEmpty(sensorData))
                    {
                        App.log.writeLine("No Data Returned to sensor " + sensor.UniqueId);
                        continue;
                    }

                    // update_interval counts from the last read, also when the value didn't change
                    sensorUpdatedAtList[sensor.UniqueId] = now;

                    if (sensorLastValues.TryGetValue(sensor.UniqueId, out string? lastValue) && lastValue == sensorData)
                    {
                        continue;
                    }

                    App.log.writeLine("Filtered Value " + sensor.UniqueId + " - " + sensorData);

                    ApiSensor senzor = new ApiSensor();
                    senzor.unique_id = sensor.UniqueId;
                    if (sensor.Icon != null)
                        senzor.icon = sensor.Icon;
                    senzor.state = SensorValueProcessor.ConvertToType(sensorData);
                    senzor.type = sensor.Type;

                    apiConnector.AddSensorData(senzor);
                    sensorLastValues[sensor.UniqueId] = sensorData;
                }
            }

            JObject? response;
            try
            {
                response = apiConnector.sendSensorBuffer();
            }
            catch (WebhookGoneException ex)
            {
                App.log.writeLine("[API] " + ex.Message);
                registerDeviceAgain();
                return;
            }

            registerMissingSensors(response);
        }

        // HA answers per sensor, re-register those it doesn't know (e.g. removed in HA)
        private void registerMissingSensors(JObject? response)
        {
            if (response == null)
            {
                return;
            }

            List<string> notRegistered = response.Properties()
                .Where(x => x.Value is JObject result && result["success"]?.Value<bool>() == false && result["error"]?["code"]?.ToString() == "not_registered")
                .Select(x => x.Name)
                .ToList();

            if (notRegistered.Count < 1)
            {
                return;
            }

            App.log.writeLine("[API] Sensors not registered in HA: " + String.Join(", ", notRegistered));
            List<SensorConfig> sensors = configuration.AllSensors.Where(x => notRegistered.Contains(x.UniqueId)).ToList();
            registerSensors(sensors);

            lock (stateLock)
            {
                // Send their values again in the next cycle
                foreach (string uniqueId in notRegistered)
                {
                    sensorLastValues.Remove(uniqueId);
                    sensorUpdatedAtList.Remove(uniqueId);
                }
            }
        }

        public void restart()
        {
            disconnect();
            connect();
        }

        public void disconnect()
        {
            if (apiConnector.connected())
            {
                App.log.writeLine("[API] Disconecting");
                App.log.writeLine("[API] Disconected");
            }
            if (apiTimer.IsEnabled)
            {
                App.log.writeLine("[API] Ping stoping");
                apiTimer.Stop();
                App.log.writeLine("[API] Ping Stopped");
            }
        }

        public void connect()
        {
            int pingLoopIndex = 0;
            do
            {
                App.log.writeLine("Waiting ntil server response!");
                pingLoopIndex++;
            } while (!Network.PingHost(apiConnector.getUrl().Host) && pingLoopIndex < 5);

            string webhookId = config.AppSettings.Settings["webhook_id"].Value;
            string secret = config.AppSettings.Settings["secret"].Value;

            if (String.IsNullOrEmpty(webhookId))
            {
                registerDevice();
            }
            else
            {
                apiConnector.setWebhookID(webhookId);
                apiConnector.setSecret(secret);
            }

            // register_sensor also updates already registered sensors, so sensors added to
            // configuration.yaml later get registered and changed names/icons get applied
            try
            {
                registerSensors(configuration.AllSensors);
            }
            catch (WebhookGoneException ex)
            {
                App.log.writeLine("[API] " + ex.Message);
                registerDeviceAgain();
            }

            apiTimer.Start();
        }

        private void registerDevice()
        {
            ApiDevice devideForRegistration = new ApiDevice()
            {
                device_name = Environment.MachineName,
                device_id = Environment.MachineName.ToLower(),
                app_id = Assembly.GetEntryAssembly().GetName().Version.ToString().ToLower(),
                app_name = Assembly.GetExecutingAssembly().GetName().Name,
                app_version = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                manufacturer = Wmic.GetValue("Win32_ComputerSystem", "Manufacturer", "root\\CIMV2"),
                model = Wmic.GetValue("Win32_ComputerSystem", "Model", "root\\CIMV2"),
                os_name = Wmic.GetValue("Win32_OperatingSystem", "Caption", "root\\CIMV2"),
                os_version = Environment.OSVersion.ToString(),
                app_data = new
                {
                    push_websocket_channel = true,
                },
                supports_encryption = false
            };
            apiConnector.RegisterDevice(devideForRegistration);

            config.AppSettings.Settings["webhook_id"].Value = apiConnector.getWebhookID();
            config.AppSettings.Settings["secret"].Value = apiConnector.getSecret();
            config.Save(ConfigurationSaveMode.Modified);

            App.log.writeLine("[API] Device registered");
        }

        // The device was removed from HA, register it again so the app keeps working without user action
        private void registerDeviceAgain()
        {
            App.log.writeLine("[API] Registering device again");
            try
            {
                registerDevice();
                registerSensors(configuration.AllSensors);
            }
            catch (Exception ex)
            {
                App.log.writeLine("[API] Device registration failed: " + ex.Message);
                return;
            }

            lock (stateLock)
            {
                apiConnector.clearSensorBuffer();
                sensorLastValues.Clear();
                sensorUpdatedAtList.Clear();
            }

            WebhookChanged?.Invoke(apiConnector.getWebhookID());
        }

        private void registerSensors(IEnumerable<SensorConfig> sensors)
        {
            foreach (SensorConfig sensor in sensors)
            {
                ApiSensor senzor = new ApiSensor();

                senzor.type = sensor.Type;
                senzor.name = sensor.Name;
                senzor.unique_id = sensor.UniqueId;
                senzor.device_class = sensor.DeviceClass;
                if (sensor.Icon != null)
                    senzor.icon = sensor.Icon;
                senzor.unit_of_measurement = sensor.UnitOfMeasurement;
                senzor.state_class = sensor.StateClass;
                senzor.entity_category = sensor.EntityCategory;
                senzor.disabled = sensor.Disabled;

                if (sensor.Type == SensorConfig.TypeBinarySensor)
                    senzor.state = false;

                try
                {
                    apiConnector.RegisterSensorData(senzor);
                }
                catch (WebhookGoneException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    App.log.writeLine("[API] Failed to register sensor " + sensor.UniqueId + ": " + ex.Message);
                }
            }
        }

        private void apiWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            // Block until done, so IsBusy really prevents overlapping runs
            try
            {
                queryAndSendSenzorData().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                App.log.writeLine("[API] Sensor update failed: " + ex.Message);
            }
        }

        private void updateSensors(object? sender, EventArgs e)
        {
            if (apiWorker.IsBusy != true)
            {
                apiWorker.RunWorkerAsync();
            }
        }
    }
}
