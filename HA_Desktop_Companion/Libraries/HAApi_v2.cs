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
using System.Xml;

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
                resultUrl = resultUrl.Substring(0, resultUrl.Length - 1);

            if (!resultUrl.StartsWith("http"))
                resultUrl = "http://" + resultUrl;

            return resultUrl;
        }

        private JsonObject sendHaRequest(string token, string webhookUrlEndpoint, object body)
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

            log.Write("API CODE <- " + response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<JsonObject>("{}");
            }

            string result = response.Content.ReadAsStringAsync().Result;

            if (String.IsNullOrEmpty(result))
            {
                log.Write("API <- No BODY");
                return JsonSerializer.Deserialize<JsonObject>("{}");
            }

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

                JsonObject response = sendHaRequest(api_token, "/api/mobile_app/registrations", body); ;
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

        public bool updateHaDevice(string deviceName = "", string model = "", string manufacturer = "", string osVersion = "")
        {
            string appVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

            Dictionary<string, object> data = new()
            {
                { "app_version", appVersion },
                { "device_name", deviceName },
                { "manufacturer", manufacturer },
                { "model", model },
                { "os_version", osVersion },
                //{ "app_data", new { push_websocket_channel = true,}},
            };

            Dictionary<string, object> body = new()
            {
                {"data", data },
                {"type", "update_registration"},
            };

            JsonObject response = sendHaRequest(api_token, "/api/webhook/" + api_webhook_id, body);
            log.Write(JsonSerializer.Serialize(response));

            //TODO: Somehow Validate
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

            System.Threading.Thread.Sleep(1000);
            JsonObject response = sendHaRequest(api_token, "/api/webhook/" + api_webhook_id, body);
            log.Write(JsonSerializer.Serialize(response));

            if ((bool) response["success"] == true)
            {
                log.Write("API = " + uniqueId + " - " + "Sensor Sucesfully Registered");
                return true;
            }

            return false;
        }

        public bool validateWebhookId(string deviceName, string model, string manufacturer, string osVersion)
        {
            log.Write(api_token);
            log.Write(api_webhook_id);

            string appVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();

            Dictionary<string, object> data = new()
            {
                { "app_version", appVersion },
                { "device_name", deviceName },
                { "manufacturer", manufacturer },
                { "model", model },
                { "os_version", osVersion },
                //{ "app_data", new { push_websocket_channel = true,}},
            };

            Dictionary<string, object> body = new()
            {
                {"data", data },
                {"type", "update_registration"},
            };

            JsonObject response = sendHaRequest(api_token, "/api/webhook/" + api_webhook_id, body);
            log.Write(JsonSerializer.Serialize(response));

            if (JsonSerializer.Serialize(response) != "{}")
            {
                log.Write("API = webhook valid");
                return true;
            }

            log.Write("API = webhook id invalid");
            return false;

            if (registerHaEntiti(deviceName, model, manufacturer, osVersion))
            {
                return true;
            }
            return false;
        }

    
        public bool addHaEntitiData(string uniqueId, object state, string type = "sensor", string icon = "")
        {
            try {
                /*Dictionary<string, object> actualFrame = entitiesData.ConvertAll(x => (Dictionary<string, object>)x).ToList().Find(o => o["unique_id"] == uniqueId);
                if (actualFrame != null)
                {
                    //TODO: Leave newest value;
                    log.Write("API DATA SKIP -> " + uniqueId + " - " + actualFrame["state"] + ">" + state + "-" + " Only one Unique_ID alowed !");
                    return;
                }*/

                Dictionary<string, object> oldFrame = entitiesDataOld.Find(o => o["unique_id"] == uniqueId);
                if (oldFrame != null && oldFrame["state"].ToString() == state.ToString())
                {
                    log.Write("API/ADD/SKIP/SAME/[" + uniqueId + "]'" + state + "'=='" + oldFrame["state"]+"'");
                    return false;
                }

                Dictionary<string, object> data = new()
                {
                    { "unique_id", uniqueId },
                    { "type", type },
                    { "state", state }
                };

                if (!String.IsNullOrEmpty(icon))
                    data.Add("icon", icon);

                log.Write("API/ADD/DATA[" + uniqueId + "]'" + state + "'");
                entitiesData.Add(data);
                return true;
            } catch (Exception ex)
            {
                log.Write("API/ADD/ERROR[" + uniqueId + "]" + api_base_url + " ");
                log.Write(ex.Message);
                return false;
            }
            return false;
        }

        public bool sendHaEntitiData()
        {
            Dictionary<string, object> body = new()
            {
                {"data", entitiesData },
                {"type", "update_sensor_states"},
            };

            JsonObject response = sendHaRequest(api_token, "/api/webhook/" + api_webhook_id, body);

            /*if ((string)response["success"] == "True")
            return true;

            return false;*/
            entitiesDataOld = entitiesData.ConvertAll(x => (Dictionary<string, object>)x).Concat(entitiesDataOld).GroupBy(x => x["unique_id"]).Select(x => x.Last()).ToList();
            entitiesData.Clear();

            return true;
        }

        public bool validateHaUrl()
        {
            //TODO: MOVE TO API class
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("GET"), api_base_url + "/api");
                using var httpClient = new HttpClient();
                var response = httpClient.Send(request);
                
                log.Write("API -> connection Verified:" + response.IsSuccessStatusCode);
                return true;
           
            }
            catch (WebException ex)
            {
                log.Write("API -> Failed to connect to:" + api_base_url + " " + ex.Message);
                return false;
            }
            return false;
        }
    }
}
