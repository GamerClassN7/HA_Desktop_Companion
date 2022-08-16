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
        public string remote_ui_url = "";
        public string cloudhook_url = "";

        public Dictionary<string, object> entities = new();

        public HAApi(string baseUrl, string apiToken, string deviceID, string deviceName, string model, string manufactorer, string os, string osVersion)
        {
            base_url = baseUrl;
            token = apiToken;
            var response = HADevicRegistration(deviceID, deviceName, model, manufactorer, os, osVersion);
            webhook_id = response["webhook_id"].ToString();
            remote_ui_url = (string) response["remote_ui_url"];
            cloudhook_url = (string) response["cloudhook_url"];
        }

        public HAApi(string baseUrl, string apiToken, string webhookId, string remoteUiUrl = "", string cloudhookUrl = "")
        {
            base_url = baseUrl;
            webhook_id = webhookId;
            token = apiToken;
            remote_ui_url = remoteUiUrl;
            cloudhook_url = cloudhookUrl;
        }

        private JsonObject HARequest(string token, string webhookUrlEndpoint, object body)
        {
            try
            {
                string rootUrl = base_url;

                /*if (remote_ui_url != "")
                    rootUrl = remote_ui_url;*/

                using var httpClient = new HttpClient();

                using var request = new HttpRequestMessage(new HttpMethod("POST"), rootUrl + webhookUrlEndpoint);
                request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
                request.Content = JsonContent.Create(body);
                Debug.WriteLine("HTTP SEND:" + JsonSerializer.Serialize(body));
                /*using (StreamWriter sw = File.AppendText(".\\log.txt"))
                {
                    sw.WriteLine(JsonSerializer.Serialize(body));
                }*/

                var response = httpClient.Send(request);
                Debug.WriteLine("HTTP CODE:" + response.StatusCode.ToString());
                 /*using (StreamWriter sw = File.AppendText(".\\log.txt"))
                {
                    sw.WriteLine(response.StatusCode.ToString());
                }*/

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    return JsonSerializer.Deserialize<JsonObject>("{}");

                string result = response.Content.ReadAsStringAsync().Result;
                Debug.WriteLine("HTTP RECIEVE:" + result);
                 /*using (StreamWriter sw = File.AppendText(".\\log.txt"))
                {
                    sw.WriteLine(result);
                }*/

                var values = JsonSerializer.Deserialize<JsonObject>(result)!;
                return values;
               
            } catch (Exception e) {
                using StreamWriter sw = File.AppendText(".\\log.txt");
                sw.WriteLine(e.Message);
                //throw new InvalidOperationException(@"HA Request Failed!", e);
            }

            return JsonSerializer.Deserialize<JsonObject>("{}");
        }

        public JsonObject HADevicRegistration(string deviceID, string deviceName, string model, string manufacturer, string os, string osVersion)
        {
            string app_version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            var body = new
            {
                device_id = deviceID,
                app_id = "ha_desktop_companion",
                app_name = "HA Desktop Companion",
                app_version,
                device_name = deviceName,
                manufacturer,
                model,
                os_name = os,
                os_version = osVersion,
                supports_encryption = false,
            };

            return HARequest(token, "/api/mobile_app/registrations", body);
        }

        public JsonObject HASenzorRegistration(string uniqueID, string uniqueName, string state, string deviceClass = "", string units= "", string icon = "", string entityCategory = "")
        {
            Dictionary<string, string> data = new()
            {
                { "unique_id", uniqueID },
                { "name", uniqueName },
                { "type", "sensor" },
                { "state", state }
            };

            if (deviceClass != "")
                data.Add("device_class", deviceClass);

            if (units != "")
                data.Add("unit_of_measurement", units);

            if (entityCategory != "")
                data.Add("entity_category", entityCategory);

            if (icon != "")
                data.Add("icon", icon);

            object body = new {
                data,
                type = "register_sensor"
            };

            Dictionary<string, string> entiry = new Dictionary<string, string>(data);
            entiry.Add("last_value", state);
            entities.Add(uniqueID, entiry);

            System.Threading.Thread.Sleep(1000);
            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }

        public JsonObject HASendSenzorData(string uniqueID, string state)
        {
            Dictionary<string, string> entityTemplate = new() { };
            if (entities.ContainsKey(uniqueID))
            {
                entityTemplate = (Dictionary<string, string>) entities[uniqueID];
                if (entityTemplate["last_value"] == state) {
                    Debug.WriteLine("Same Value as last one skipping");
                    return JsonSerializer.Deserialize<JsonObject>("{}");
                }
                entityTemplate["last_value"] = state;
            }

            Dictionary<string, string> data = new()
            {
                { "unique_id", uniqueID },
                { "type", "sensor" },
                { "state", state }
            };

            if (entityTemplate.ContainsKey("icon") && entityTemplate["icon"] != "")
                data.Add("icon", entityTemplate["icon"]);

            var body = new
            {
                data = new[] { data, },
                type = "update_sensor_states"
            };

            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }
    }
}
