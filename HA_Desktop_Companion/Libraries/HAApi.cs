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
        public string base_url              = "";
        public string token                 = "";
        public string webhook_id            = "";
        public string remote_ui_url         = "";
        public string cloudhook_url         = "";

        private bool debug                  = false;

        private string senzorBackupFilePath = @".\senzors_backup.json";

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
             GetHASenzorsCatalog();
        }

        public void enableDebug(bool debug = false)
        {
            debug = debug;
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

                if (JsonSerializer.Serialize(body) == "{}"){
                    Debug.WriteLine("SENZOR READ ERROR ");
                    using (StreamWriter sw = File.AppendText(".\\log.txt"))
                    {
                        sw.WriteLine("SENZOR READ ERROR" + JsonSerializer.Serialize(body));
                    }
                    return JsonSerializer.Deserialize<JsonObject>("{}");
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
            string app_name = Assembly.GetExecutingAssembly().GetName().Name;

            var body = new
            {
                device_id = deviceID,
                app_id = app_name.ToLower(),
                app_name = app_name,
                app_version,
                device_name = deviceName,
                manufacturer,
                model,
                os_name = os,
                os_version = osVersion,
                supports_encryption = false,
                app_data = new {
                    push_websocket_channel = true,
                }
            };

            JsonObject response = HARequest(token, "/api/mobile_app/registrations", body);
            return response;
        }

        public void GetHASenzorsCatalog()
        {
            if (File.Exists(senzorBackupFilePath))
            {
                string fileContent = File.ReadAllText(senzorBackupFilePath, System.Text.Encoding.UTF8);
                //Debug.WriteLine("SENSOR BACKUP LOADET:" + fileContent);

                JsonObject backup = JsonSerializer.Deserialize<JsonObject>(fileContent);
                foreach (var senzor in backup)
                {
                    Dictionary<string, object> entiry = new()
                    {
                        { "last_value", senzor.Value["last_value"] },
                        /*{ "last_time", senzor.Value["last_time"] },*/
                        { "icon", senzor.Value["icon"] }
                    };

                    entitiesCatalog.Add(senzor.Key,  entiry);
                }
            }
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

            Debug.WriteLine("SENSOR BACKUP SAVED:" + JsonSerializer.Serialize(entitiesCatalog));
            File.WriteAllText(senzorBackupFilePath, JsonSerializer.Serialize(entitiesCatalog), System.Text.Encoding.UTF8);

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
        
        private object HAGenericSenzorDataBody(string uniqueID, object state, string type = "sensor",string icon = "")
        {
            Debug.WriteLine(uniqueID);
            Dictionary<string, object> entityTemplate = new() { };

            if (entitiesCatalog.ContainsKey(uniqueID))
            {
                entityTemplate = (Dictionary<string, object>)entitiesCatalog[uniqueID];
                if (entityTemplate["last_value"] == state)
                {
                    Debug.WriteLine("Same Value as last one skipping");
                    return JsonSerializer.Deserialize<JsonObject>("{}");
                }

                /*if (entityTemplate["last_time"] == state)
                {
                    Debug.WriteLine("update intervall not reached");
                    return JsonSerializer.Deserialize<JsonObject>("{}");
                }*/

                entityTemplate["last_value"] = state;
                /*entityTemplate["last_time"] = Convert.ToString((int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);*/
                entitiesCatalog[uniqueID] = entityTemplate;
            }

            Dictionary<string, object> data = new()
            {
                { "unique_id", uniqueID },
                { "type", type },
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
            object body = HAGenericSenzorDataBody(uniqueID, state, "binary_sensor");

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

        public JsonObject HASendSenzorLocation()
        {
            string[] latLon = Sensors.queryLocationByIP().Split(",");

            Debug.WriteLine(float.Parse(latLon[0].Replace(".", ",")));
            Debug.WriteLine(float.Parse(latLon[1].Replace(".", ",")));

            Dictionary<string, object> data = new() {
                { "gps" , new[] {
                    float.Parse(latLon[0].Replace(".", ",")),
                    float.Parse(latLon[1].Replace(".", ","))
                }},
                {"gps_accuracy" , 3000 }
            };

            var body = new
            {
                data = data,
                type = "update_location"
            };

            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }
    }
}
