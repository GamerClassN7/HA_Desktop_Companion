using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace HA_Desktop_Companion
{
    public class HAApi
    {
        public string base_url = "";
        public string token = "";
        public string webhook_id = "";

        public HAApi(string baseUrl, string apiToken, string deviceID, string deviceName, string model, string manufactorer, string os, string osVersion)
        {
            base_url = baseUrl;
            token = apiToken;
            webhook_id = HADevicRegistration(deviceID, deviceName, model, manufactorer, os, osVersion)["webhook_id"].ToString();
        }

        public HAApi(string baseUrl, string apiToken, string webhookId)
        {
            base_url = baseUrl;
            webhook_id = webhookId;
            token = apiToken;
        }

        private JsonObject HARequest(string token, string webhookUrlEndpoint, object body)
        {
            try
            {
                using var httpClient = new HttpClient();

                using var request = new HttpRequestMessage(new HttpMethod("POST"), base_url + webhookUrlEndpoint);
                request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
                request.Content = JsonContent.Create(body);
                Debug.WriteLine("HTTP SEND:" + JsonSerializer.Serialize(body));

                var response = httpClient.Send(request);
                Debug.WriteLine("HTTP CODE:" + response.StatusCode.ToString());

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    return JsonSerializer.Deserialize<JsonObject>("{}");

                string result = response.Content.ReadAsStringAsync().Result;
                Debug.WriteLine("HTTP RECIEVE:" + result);

                var values = JsonSerializer.Deserialize<JsonObject>(result)!;
                return values;
               
            } catch (Exception e) {
                using (StreamWriter sw = File.AppendText(".\\log.txt"))
                {
                    sw.WriteLine(e.Message);
                }
                //throw new InvalidOperationException(@"HA Request Failed!", e);
            }

            return JsonSerializer.Deserialize<JsonObject>("{}");
        }

        public JsonObject HADevicRegistration(string deviceID, string deviceName, string model, string manufactorer, string os, string osVersion)
        {
            string app_version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            var body = new
            {
                device_id = deviceID,
                app_id = "ha_desktop_companion",
                app_name = "HA Desktop Companion",
                app_version = app_version,
                device_name = deviceName,
                manufacturer = manufactorer,
                model = model,
                os_name = os,
                os_version = osVersion,
                supports_encryption = false,
            };

            return HARequest(token, "/api/mobile_app/registrations", body);
        }

        public JsonObject HASenzorRegistration(string uniqueID, string uniqueName, string state, string deviceClass = "", string units= "", string icon = "", string entityCategory = "")
        {

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("unique_id", uniqueID);
            data.Add("name", uniqueName);
            data.Add("type", "sensor");
            data.Add("state", state);

            if (deviceClass != "")
                data.Add("device_class", deviceClass);

            if (units != "")
                data.Add("unit_of_measurement", units);

            if (entityCategory != "")
                data.Add("entity_category", entityCategory);

            if (icon != "")
                data.Add("icon", icon);

            var body = new {
                data = data,
                type = "register_sensor"
            };


            System.Threading.Thread.Sleep(1000);
            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }

        public JsonObject HASendSenzorData(string uniqueID, string state)
        {

            var body = new {
                data = new[] {
                    new {
                        unique_id = uniqueID,
                        state =state,
                        type = "sensor",
                    }
                },
                type = "update_sensor_states"
            };

            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }
    }
}
