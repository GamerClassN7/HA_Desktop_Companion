using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
using System.Reflection;
using System.Windows;
using System.Diagnostics;

namespace HA_Desktop_Companion.Libraries
{
    public class HAApi_v2
    {
        public string api_base_url = null;
        public string api_cloudhook_url = null;
        public string api_remote_ui_url = null;
        public string api_token = null;
        public string api_webhook_id = null;

        private List<object> entitiesData = new();
        private List<Dictionary<string, object>> entitiesDataOld = new();

        private Logging log;

        public HAApi_v2(string apiBaseUrl, string apiToken, Logging logInstance, string apiWebHookId = "", string apiRemoteUiUrl = "", string apiCloudhookUrl = "")
        {
            api_base_url = apiBaseUrl;

            if (!String.IsNullOrEmpty(apiCloudhookUrl))
                api_cloudhook_url = apiCloudhookUrl;

            if (!String.IsNullOrEmpty(apiRemoteUiUrl))
                api_remote_ui_url = apiRemoteUiUrl;

            api_token = apiToken;
            api_webhook_id = apiWebHookId;
            log = logInstance;
        }

        public void enableDebug(bool debug = false)
        {
           
        }

        private string resolveHaUri()
        {
            string resultUrl = api_base_url;
            //TODO: Cascade reference of url

            if (!string.IsNullOrEmpty(api_remote_ui_url))
                resultUrl = api_remote_ui_url;

            if (resultUrl.EndsWith("/"))
            {
                resultUrl = resultUrl.Substring(0, resultUrl.Length - 1);
            }

            return resultUrl;
        }

        private async Task<JsonObject> sendHaRequest(string token, string webhookUrlEndpoint, object body)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping};

            if (JsonSerializer.Serialize(body) == "{}")
                return JsonSerializer.Deserialize<JsonObject>("{}");
            
            string rootUrl = resolveHaUri();
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("POST"), rootUrl + webhookUrlEndpoint);
            request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
            request.Content = JsonContent.Create(body);
            log.Write("API -> " + JsonSerializer.Serialize(body, options));

            using var httpClient = new HttpClient();
            var response = httpClient.Send(request);

            if (!response.IsSuccessStatusCode)
            {
                log.Write("API <- " + response.StatusCode);
                return JsonSerializer.Deserialize<JsonObject>("{}");
            }

            string result = response.Content.ReadAsStringAsync().Result;
            log.Write("API <- " +JsonSerializer.Serialize(result, options));

            return JsonSerializer.Deserialize<JsonObject>(result);
        }

        public bool registerHaDevice(string deviceID, string deviceName, string model, string manufacturer, string os, string osVersion)
        {
            if (String.IsNullOrEmpty(api_webhook_id))
            {
                string appVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
                string appName = Assembly.GetExecutingAssembly().GetName().Name;

                Dictionary<string, object> body = new()
                {
                    { "device_id", deviceID },
                    { "app_id", appName.ToLower() },
                    { "app_name", appName },
                    { "app_version", appVersion },
                    { "device_name", deviceName },
                    { "manufacturer", manufacturer },
                    { "model", model },
                    { "os_name", os },
                    { "os_version", osVersion },
                    { "supports_encryption", false },
                    { "app_data", new {
                        push_websocket_channel = true,
                    }},
                };

                log.Write(JsonSerializer.Serialize(body));
                Task<JsonObject> task = sendHaRequest(api_token, "/api/mobile_app/registrations", body);
                task.Wait();

                JsonObject response = task.Result;
                log.Write(JsonSerializer.Serialize(response));

                if (JsonSerializer.Serialize(response) == "{}")
                    return false;

                api_webhook_id = response["webhook_id"].ToString();

                if (!String.IsNullOrEmpty((string) response["cloudhook_url"]))
                    api_cloudhook_url = response["cloudhook_url"].ToString();

                if (!String.IsNullOrEmpty((string) response["remote_ui_url"]))
                    api_remote_ui_url = response["remote_ui_url"].ToString();

                return true;
           }

           return true;
        }

        public bool registerHaEntiti(string uniqueId, string name, object state, string type = "sensor", string deviceClass = "", string entityCategory = "", string icon = "", string unitOfMeasurement = "")
        {
            Dictionary<string, object> data = new()
            {
                { "unique_id", uniqueId },
                { "name", name },
                { "type", type },
                { "state", state }
            };

            if (!String.IsNullOrEmpty(deviceClass))
                data.Add("device_class", deviceClass);

            if (!String.IsNullOrEmpty(unitOfMeasurement))
                data.Add("unit_of_measurement", unitOfMeasurement);

            if (!String.IsNullOrEmpty(entityCategory))
                data.Add("entity_category", entityCategory);

            if (!String.IsNullOrEmpty(icon))
                data.Add("icon", icon);

            Dictionary<string, object> body = new()
            {
                {"data", data },
                {"type", "register_sensor"},
            };

            Task<JsonObject> task = sendHaRequest(api_token, "/api/webhook/" + api_webhook_id, body);
            System.Threading.Thread.Sleep(1000);
            task.Wait();

            JsonObject response = task.Result;
            log.Write(JsonSerializer.Serialize(response));

            if ((bool) response["success"] == true)
            {
                log.Write("API = " + uniqueId + " - " + "Sensor Sucesfully Registered");
                return true;
            }

            return false;
        }
    
        public void addHaEntitiData(string uniqueId, object state, string type = "sensor", string icon = "")
        {
            Dictionary<string, object> oldFrame = entitiesDataOld.Find(o => o["unique_id"] == uniqueId);
         
            if (oldFrame != null && oldFrame["state"].ToString() == state.ToString())
            {
                log.Write("API DATA SKIP -> " + uniqueId + " - " + state + "==" + oldFrame["state"]  + "-" + "SAME !");
                return;
            }

            Dictionary<string, object> data = new()
            {
                { "unique_id", uniqueId },
                { "type", type },
                { "state", state }
            };

            if (!String.IsNullOrEmpty(icon))
                data.Add("icon", icon);

            log.Write("API DATA -> "+ uniqueId + " - " + state);
            entitiesData.Add(data);
        }

        public bool sendHaEntitiData()
        {
            Dictionary<string, object> body = new()
            {
                {"data", entitiesData },
                {"type", "update_sensor_states"},
            };

            Task<JsonObject> task = sendHaRequest(api_token, "/api/webhook/" + api_webhook_id, body);
    
            JsonObject response = task.Result;

            /*if ((string)response["success"] == "True")
            return true;

            return false;*/
            entitiesDataOld = entitiesData.ConvertAll(x => (Dictionary<string, object>)x).Union(entitiesDataOld).ToList();
            entitiesData.Clear();

            return true;
        }
    }
}
