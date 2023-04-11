using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using HA.@class.HomeAssistant;
using HA.@class.HomeAssistant.Objects;

namespace HA
{
    /// <summary>
    /// Interaction logic for App.xaml test 
    /// </summary>
    public partial class App : Application
    {
        static HomeAssistantAPI ha;
        static DispatcherTimer? update = null;
        void App_Startup(object sender, StartupEventArgs e)
        {
           
        }

        public static bool Start()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            string token = config.AppSettings.Settings["token"].Value;
            string url = config.AppSettings.Settings["url"].Value;
            string webhookId = config.AppSettings.Settings["webhookId"].Value;
            string secret = config.AppSettings.Settings["secret"].Value;

            try
            {
                ha = new HomeAssistantAPI(url, token);
            } catch {
                return false;
            }

            try
            {
                ha.GetVersion();
            }
            catch
            {
                return false;
            }

            if (String.IsNullOrEmpty(webhookId))
            {
                HomeAssistatnDevice device = new HomeAssistatnDevice();
                device.device_id = System.Environment.MachineName;
                device.app_id = "awesome_home";
                device.app_name = "Awesome Home";
                device.app_version = "1.2.0";
                device.device_name = System.Environment.MachineName;
                device.manufacturer = "Apple, Inc.";
                device.model = "iPhone X";
                device.os_name = "Windows";
                device.os_version = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                device.supports_encryption = false;
                MessageBox.Show(ha.RegisterDevice(device));

                HomeAssistatnSensors senzor = new HomeAssistatnSensors();
                senzor.device_class = "battery";
                senzor.icon = "mdi:battery";
                senzor.name = "Battery State";
                senzor.state = "12345";
                senzor.type = "sensor";
                senzor.unique_id = "battery_state";
                senzor.unit_of_measurement = "%";
                senzor.state_class = "measurement";
                senzor.entity_category = "diagnostic";
                senzor.disabled = false;
                MessageBox.Show(ha.RegisterSensorData(senzor));
            }
            else
            {
                ha.setWebhookID(webhookId);
                ha.setSecret(secret);
            }

            update = new DispatcherTimer();
            update.Interval = TimeSpan.FromSeconds(1);
            update.Tick += UpdateSenzorTick;
            update.Start();

            return true;
        }

        public static void Stop()
        {
            if (update != null)
            {
                update.Stop();
            }
        }

        static async void UpdateSenzorTick(object sender, EventArgs e)
        {
            HomeAssistatnSensors senzor = new HomeAssistatnSensors();

            Random random = new Random();

            senzor.icon = "mdi:battery";
            senzor.state = random.Next(1, 100).ToString();
            senzor.type = "sensor";
            senzor.unique_id = "battery_state";

            ha.AddSensorData(senzor);
            ha.sendSensorBuffer();
        }
    }
}
