using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

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
                using (var httpClient = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(new HttpMethod("POST"), base_url + webhookUrlEndpoint))
                    {
                        request.Headers.TryAddWithoutValidation("Content-Type", "application/json");
                        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
                        request.Content = JsonContent.Create(body);

                        var response = httpClient.Send(request);

                        using (HttpContent content = response.Content)
                        {
                            string result = content.ReadAsStringAsync().Result;
                            var values = JsonSerializer.Deserialize<JsonObject>(result)!;
                            return values;
                        }
                    }
                }
            } catch (Exception e) {
                throw new InvalidOperationException(@"HA Request Failed!" + this.base_url, e);
            }
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

        public JsonObject HASenzorRegistration(string uniqueID, string uniqueName, string state)
        {
            System.Threading.Thread.Sleep(1000);
            var body = new {
                data = new {
                    unique_id = uniqueID,
                    name = uniqueName,
                    device_class = "battery",
                    type = "sensor",
                    state = state,
                },
                type = "register_sensor"
            };
            return HARequest(token, "/api/webhook/" + webhook_id, body);
        }

        public JsonObject HASendSenzorData(string uniqueID, string state)
        {

            var body = new {
                data = new[] {
                    new {
                        attributes = new {
                            hello = "world"
                        },
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
