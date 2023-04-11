﻿using System;
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
using HA.@class.HomeAssistant.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HA.@class.HomeAssistant
{
    public class HomeAssistantAPI
    {
        private string url = null;
        private string token = null;

        //Data From Registration
        private string webhookId = null;
        private string secret = null;

        private List<HomeAssistatnSensors> sensorsBuffer = new List<HomeAssistatnSensors>();

        public void setWebhookID(string apiWebhookId)
        {
            webhookId = apiWebhookId;
        }
        public void setSecret(string apiSecret)
        {
            secret = apiSecret;

        }

        public HomeAssistantAPI(string apiRootUrl, string apiToken)
        {
            if (!testApiUrl(apiRootUrl))
            {
                throw new Exception("unnabůle to connect to" + apiRootUrl);
            }

            url = apiRootUrl;
            token = apiToken;
        }

        public bool testApiUrl(string apiRootUrl)
        {
            return true;
        }

        private HttpContent sendApiRequest(string endpoint)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = client.GetAsync(endpoint).Result;
            if (response.IsSuccessStatusCode)
            {
                return response.Content;
                //  usergrid.ItemsSource = users;
                //.ReadAsAsync<IEnumerable<Users>>().Result
            }
            else
            {
                throw new Exception("Error Code" + response.StatusCode + " : Message - " + response.ReasonPhrase);
            }
        }

        private HttpContent sendApiPOSTRequest(string endpoint, object payload)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string content = JsonConvert.SerializeObject(payload, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }).ToString();

            Debug.WriteLine(content);
            Debug.WriteLine(webhookId);

            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            HttpResponseMessage response = client.PostAsync(endpoint, stringContent).Result;

            if (response.IsSuccessStatusCode)
            {
                return response.Content;
                //usergrid.ItemsSource = users;
                //.ReadAsAsync<IEnumerable<Users>>().Result
            }
            else
            {
                throw new Exception("Error Code" + response.StatusCode + " : Message - " + response.ReasonPhrase);
            }
        }

        public string GetVersion()
        {
            var jObject = JObject.Parse(sendApiRequest("/api/config").ReadAsStringAsync().Result);
            return jObject["version"].ToString();
        }

        public string RegisterDevice(HomeAssistatnDevice device)
        {
            //https://developers.home-assistant.io/docs/api/native-app-integration/setup
            var jObject = JObject.Parse(sendApiPOSTRequest("/api/mobile_app/registrations", device).ReadAsStringAsync().Result);

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            webhookId = jObject["webhook_id"].ToString();
            MessageBox.Show(webhookId);
            config.AppSettings.Settings["webhookId"].Value = webhookId;

            secret = jObject["secret"].ToString();
            config.AppSettings.Settings["secret"].Value = secret;

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            return jObject.ToString();
        }

        public string RegisterSensorData(HomeAssistatnSensors senzor)
        {
            HomeAssistantRequest request = new HomeAssistantRequest();
            request.SetData(senzor);
            request.SetType("register_sensor");

            var jObject = JObject.Parse(sendApiPOSTRequest("/api/webhook/" + webhookId, request).ReadAsStringAsync().Result);
            return jObject.ToString();
        }

        public void AddSensorData(HomeAssistatnSensors senzor)
        {
            sensorsBuffer.Add(senzor);
        }

        public string sendSensorBuffer()
        {
            HomeAssistantRequest request = new HomeAssistantRequest();

            request.SetData(sensorsBuffer);
            request.SetType("update_sensor_states");

            var jObject = JObject.Parse(sendApiPOSTRequest("/api/webhook/" + webhookId, request).ReadAsStringAsync().Result);
            sensorsBuffer.Clear();
            
            return jObject.ToString();
        }
    }
}
