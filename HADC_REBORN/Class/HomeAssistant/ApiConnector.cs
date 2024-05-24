using HADC_REBORN.Class.HomeAssistant.Objects;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Security.Policy;

namespace HADC_REBORN.Class.HomeAssistant
{
    public class ApiConnector
    {
        private string url;
        private string token;

        //Data From Registration
        private string webhookId = null;
        private string secret = null;

        private HttpClient client = new HttpClient();

        //Erro Handling
        private int failedAttempts = 0;
        private List<ApiSensor> sensorsBuffer = new List<ApiSensor>();

        public ApiConnector(string apiRootUrl, string apiToken)
        {
            // if (!testApiUrl(apiRootUrl))
            //{
            //   throw new Exception("unnabůle to connect to" + apiRootUrl);
            //}

            url = apiRootUrl.TrimEnd('/');
            token = apiToken;
        }

        public string getWebhookID()
        {
            return webhookId;
        }

        public bool connected()
        {
            if (failedAttempts > 5)
                return false;

            return true;
        }

        public string getSecret()
        {
            return secret;
        }

        public void setWebhookID(string apiWebhookId)
        {
            webhookId = apiWebhookId;
        }

        public void setSecret(string apiSecret)
        {
            secret = apiSecret;
        }

        public Uri getUrl()
        {
            return new Uri(url);
        }

        private HttpContent sendApiRequest(string endpoint)
        {
            inicialize();
            HttpResponseMessage response = client.GetAsync(endpoint).Result;
            if (response.IsSuccessStatusCode)
            {
                App.log.writeLine("[API] RESPONSE CODE <" + (int)response.StatusCode + "> " + response.StatusCode.ToString());

                return response.Content;
                // usergrid.ItemsSource = users;
                //.ReadAsAsync<IEnumerable<Users>>().Result
            }
            else
            {
                failedAttempts++;
                throw new Exception("Error Code" + response.StatusCode + " : Message - " + response.ReasonPhrase);
            }
        }

        private HttpContent sendApiPOSTRequest(string endpoint, object payload)
        {
            inicialize();
            string content = JsonConvert.SerializeObject(payload, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }).ToString();

            App.log.writeLine("[API] SEND:");
            App.log.writeLine(content);

            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            HttpResponseMessage response = client.PostAsync(endpoint, stringContent).Result;
            App.log.writeLine("[API] RESPONSE CODE <" + (int)response.StatusCode + "> " + response.StatusCode.ToString());

            if (response.IsSuccessStatusCode)
            {
                failedAttempts = 0;
                return response.Content;
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                failedAttempts = 6;
                throw new Exception("Error Code" + response.StatusCode + " : Message - " + response.ReasonPhrase);
            }
            else
            {
                failedAttempts++;
                throw new Exception("Error Code" + response.StatusCode + " : Message - " + response.ReasonPhrase);
            }
        }

        public string GetVersion()
        {
            var jObject = JObject.Parse(sendApiRequest("/api/config").ReadAsStringAsync().Result);
            return jObject["version"].ToString();
        }

        public string RegisterDevice(ApiDevice device)
        {
            //https://developers.home-assistant.io/docs/api/native-app-integration/setup
            var jObject = JObject.Parse(sendApiPOSTRequest("/api/mobile_app/registrations", device).ReadAsStringAsync().Result);
            webhookId = jObject["webhook_id"].ToString();
            secret = jObject["secret"].ToString();

            return jObject.ToString();
        }

        public string RegisterSensorData(ApiSensor senzor)
        {
            ApiRequest request = new ApiRequest();
            request.SetData(senzor);
            request.SetType("register_sensor");

            var jObject = JObject.Parse(sendApiPOSTRequest("/api/webhook/" + webhookId, request).ReadAsStringAsync().Result);
            return jObject.ToString();
        }

        public void AddSensorData(ApiSensor senzor)
        {
            sensorsBuffer.Add(senzor);
        }

        public string sendSensorBuffer()
        {
            if (sensorsBuffer.Count < 1)
            {
                App.log.writeLine("No data to send!");
                return "";
            }

            ApiRequest request = new ApiRequest();

            request.SetData(sensorsBuffer);
            request.SetType("update_sensor_states");
            JObject jObject = new JObject();

            try
            {
                jObject = JObject.Parse(sendApiPOSTRequest("/api/webhook/" + webhookId, request).ReadAsStringAsync().Result);
                Debug.Write(jObject.ToString());
                sensorsBuffer.Clear();
                failedAttempts = 0;
            }
            catch (Exception ex)
            {
                failedAttempts++;
                App.log.writeLine("[API] " + ex.Message);
            }

            return jObject.ToString();
        }

        private void inicialize(){
            if (client == null)
            {
                return;
            }

            client = new HttpClient();
            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            
        }
    }
}
