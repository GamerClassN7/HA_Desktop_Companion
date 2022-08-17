using HA_Desktop_Companion.Libraries;
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
        private bool debug = false;

        public Dictionary<string, object> entitiesCatalog = new();

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

        public void enableDebug()
        {
            debug = true;
        }
        
        private string HAResolveUri()
        {
            string resultUrl = base_url;
            //TODO: Cascade reference of url

            if (!string.IsNullOrEmpty(remote_ui_url))
                resultUrl = remote_ui_url;

            if (resultUrl.ToString().EndsWith("/"))
            {
                resultUrl = resultUrl.Substring(0, resultUrl.Length - 1);
            }

            return resultUrl;
        }
        
        private JsonObject HARequest(string token, string webhookUrlEndpoint, object body)
        {
            try
            {
                string rootUrl = HAResolveUri();

                using var httpClient = new HttpClient();

                using var request = new HttpRequestMessage(new HttpMethod("POST"), rootUrl + webhookUrlEndpoint);
                request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
                request.Content = JsonContent.Create(body);

                Debug.WriteLine("HTTP SEND:" + JsonSerializer.Serialize(body));
                using (StreamWriter sw = File.AppendText(".\\log.txt"))
                {
                    sw.WriteLine(JsonSerializer.Serialize(body));
                }

                var response = httpClient.Send(request);
                Debug.WriteLine("HTTP CODE:" + response.StatusCode.ToString());
                using (StreamWriter sw = File.AppendText(".\\log.txt"))
                {
                    sw.WriteLine(response.StatusCode.ToString());
                }

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    return JsonSerializer.Deserialize<JsonObject>("{}");

                string result = response.Content.ReadAsStringAsync().Result;
                Debug.WriteLine("HTTP RECIEVE:" + result);
                using (StreamWriter sw = File.AppendText(".\\log.txt"))
                {
                    sw.WriteLine(result);
                }

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

            JsonObject response = HARequest(token, "/api/mobile_app/registrations", body);
            return response;
        }

        private object HAGenericSenzorRegistrationBody(string unique_id, string name, object state, string type = "sensor", string device_class = "", string entity_category = "", string icon = "", string unit_of_measurement = "" )
        {
            Dictionary<string, object> data = new ()
            {
                { "unique_id", unique_id },
                { "name", name },
                { "type", type },
                { "state", state }
            };

            if (device_class != "")
                data.Add("device_class", device_class);

            if (unit_of_measurement != "")
                data.Add("unit_of_measurement", unit_of_measurement);

            if (entity_category != "")
                data.Add("entity_category", entity_category);

            if (icon != "")
                data.Add("icon", icon);

            object body = new
            {
                data,
                type = "register_sensor"
            };

            //Add Entity to Catalog
            Dictionary<string, object> entiry = new()
            {
                { "last_value", state }
            };

            if (icon != "")
                entiry.Add("icon", icon);

            entitiesCatalog.Add(unique_id, entiry);

            return body;
        }
        
        public JsonObject HASenzorRegistration(string uniqueID, string uniqueName, string state, string deviceClass = "", string units = "", string icon = "", string entityCategory = "")
        {
            object body = HAGenericSenzorRegistrationBody(uniqueID, uniqueName, state, "sensor", deviceClass, entityCategory, icon, units);

            System.Threading.Thread.Sleep(1000);
            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }

        public JsonObject HASenzorRegistration(string uniqueID, string uniqueName, bool state = false, string deviceClass = "", string units = "", string icon = "", string entityCategory = "")
        {
            object body = HAGenericSenzorRegistrationBody(uniqueID, uniqueName, state, "binary_sensor", deviceClass, entityCategory, icon, units);

            System.Threading.Thread.Sleep(1000);
            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }

        public JsonObject HASenzorRegistration(string uniqueID, string uniqueName, int state, string deviceClass = "", string units = "", string icon = "", string entityCategory = "")
        {
            object body = HAGenericSenzorRegistrationBody(uniqueID, uniqueName, state, "sensor", deviceClass, entityCategory, icon, units);

            System.Threading.Thread.Sleep(1000);
            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }
        
        private object HAGenericSenzorDataBody(string uniqueID, object state, string icon = "")
        {
            Dictionary<string, object> entityTemplate = new() { };

            if (entitiesCatalog.ContainsKey(uniqueID))
            {
                entityTemplate = (Dictionary<string, object>)entitiesCatalog[uniqueID];
                if (entityTemplate["last_value"] == state)
                {
                    Debug.WriteLine("Same Value as last one skipping");
                    return JsonSerializer.Deserialize<JsonObject>("{}");
                }
                entityTemplate["last_value"] = state;
                entitiesCatalog[uniqueID] = entityTemplate;
            }

            Dictionary<string, object> data = new()
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

            return body;
        }

        public JsonObject HASendSenzorData(string uniqueID, string state)
        {
           object body = HAGenericSenzorDataBody(uniqueID, state);

            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }

        public JsonObject HASendSenzorData(string uniqueID, bool state)
        {
            object body = HAGenericSenzorDataBody(uniqueID, state);

            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }

        public JsonObject HASendSenzorData(string uniqueID, int state)
        {
            object body = HAGenericSenzorDataBody(uniqueID, state);

            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }

        public JsonObject HASendSenzorData(string uniqueID, double state)
        {
            object body = HAGenericSenzorDataBody(uniqueID, (float) state);

            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }

        /*public JsonObject HASendSenzorLocation()
        {
            Dictionary<string, object> data = new()
            {
                { "gps", new[] {
                    float.Parse(Senzors.queryLocationByIP().Split(",")[0]),
                    float.Parse(Senzors.queryLocationByIP().Split(",")[1])
                }},
            };

            var body = new
            {
                data = new[] { data, },
                type = "update_location"
            };

            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }*/
    }
}
