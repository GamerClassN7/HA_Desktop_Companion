﻿using HA.Class.HomeAssistant.Objects;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HA.Class.HomeAssistant
{
    public class HomeAssistantWS
    {
        private string url = null;
        private string token = null;
        private string webhook = null;

        private ClientWebSocket socket = new ClientWebSocket();
        private byte[] buffer = new byte[2048];
        private int interactions = 1;

        private bool isSubscribed = false;
        private bool isPingEnabled = false;
        private bool isConnected = false;
        private int retryCount = 0;

        static DispatcherTimer updatePingTimer = new DispatcherTimer();

        public HomeAssistantWS(string apiUrl, string webhookId, string apiToken)
        {
            url = apiUrl;
            token = apiToken;
            webhook = webhookId;

            try
            {
                registerAsync();
            } catch (Exception ex) {
                isSubscribed = false;
                isPingEnabled = false;
                isConnected = false;
            }
        }

        private async Task registerAsync()
        {
            try
            {
                Uri wsAddress = new Uri(url + "api/websocket");
                var exitEvent = new ManualResetEvent(false);
                socket.Options.KeepAliveInterval = TimeSpan.Zero;

                socket.ConnectAsync(wsAddress, CancellationToken.None).Wait();

                Task<JObject> initialization = RecieveAsync();
                initialization.Wait();

                if (initialization.Result["type"].ToString() != "auth_required")
                {
                    Debug.WriteLine("auth Failed");
                    return;
                }

                HAWSAuth authObj = new HAWSAuth() { };
                authObj.access_token = token;

                JObject registration = sendAndRecieveAsync(authObj);
                if (registration["type"].ToString() != "auth_ok")
                {
                    Debug.WriteLine("WS Auth error");
                    return;
                }
                Debug.WriteLine("WS Auth OK");

                HAWSRequest subscribeObj = new HAWSRequest { };
                subscribeObj.id = interactions;
                subscribeObj.webhook_id = webhook;
                subscribeObj.type = "mobile_app/push_notification_channel";

                JObject subscription = sendAndRecieveAsync(subscribeObj);
                if (bool.Parse((string)subscription["success"]) != true)
                {
                    Debug.WriteLine("WS subscription Failed");
                    return;
                }
                Debug.WriteLine("WS subscription OK");
                isSubscribed = true;
                isPingEnabled = true;

                StartPingAsyncTask();
                d
                isConnected = true;
                retryCount = 0;

                ReceiveLoopAsync();

            }
            catch (Exception ex)
            {
                Debug.WriteLine("WS error " + ex.Message);
                Close();
                if (retryCount >= 5)
                {
                    registerAsync();
                }
            }
        }

        private JObject sendAndRecieveAsync(dynamic payloadObj)
        {
            string JSONPayload = JsonConvert.SerializeObject(payloadObj, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }).ToString();

            Debug.WriteLine("SEND");
            Debug.WriteLine(JSONPayload);

            ArraySegment<byte> BYTEPayload = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JSONPayload));

            socket.SendAsync(BYTEPayload, WebSocketMessageType.Text, true, CancellationToken.None).Wait();

            string JSONRecievedpayload =  "";
      
                Debug.WriteLine("SEND/RECIEVING");
                interactions = interactions + 1;

                WebSocketReceiveResult result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;

                JSONRecievedpayload = Encoding.UTF8.GetString(buffer, 0, result.Count);

                Debug.WriteLine("RECIEVE");
                Debug.WriteLine(JSONRecievedpayload);
         

            return JObject.Parse(JSONRecievedpayload);
        }

        private async Task<JObject> RecieveAsync()
        {
            WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            string JSONRecievedpayload = Encoding.UTF8.GetString(buffer, 0, result.Count);

            Debug.WriteLine("RECIEVE");
            Debug.WriteLine(JSONRecievedpayload);

            return JObject.Parse(JSONRecievedpayload);
        }

        private async Task Send(dynamic payloadObj)
        {
            string JSONPayload = JsonConvert.SerializeObject(payloadObj, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }).ToString();

            Debug.WriteLine("SEND");
            Debug.WriteLine(JSONPayload);
            interactions = interactions + 1;

            ArraySegment<byte> BYTEPayload = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JSONPayload));

            socket.SendAsync(BYTEPayload, WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }

        public bool getConectionStatus()
        {
             return (isConnected && isSubscribed);
        }
        private async Task StartPingAsyncTask()
        {
            Debug.WriteLine("Initializing Ping");
            updatePingTimer = new DispatcherTimer();
            updatePingTimer.Interval = TimeSpan.FromMinutes(30);
            updatePingTimer.Tick += UpdatePingTick;
            updatePingTimer.Start();
        }

        private async void UpdatePingTick(object sender, EventArgs e)
        {
            if (isPingEnabled == true)
            {

                HAWSPing pingObj = new HAWSPing { };
                pingObj.type = "ping";
                pingObj.id = interactions;

                Send(pingObj);
            }
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                Debug.WriteLine("WS RECEEVE LOOP STARTED");
                var localBuffer = new ArraySegment<byte>(new byte[2048]);
                do
                {
                    WebSocketReceiveResult localResult;
                    using (var ms = new MemoryStream())
                    {
                        do
                        {
                            localResult = await socket.ReceiveAsync(localBuffer, CancellationToken.None);
                            ms.Write(localBuffer.Array, localBuffer.Offset, localResult.Count);
                        } while (!localResult.EndOfMessage);

                        if (localResult.MessageType == WebSocketMessageType.Close)
                            break;

                        ms.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            string jsonPayload = await reader.ReadToEndAsync();
                            Debug.WriteLine("WS RECEEVED");
                            Debug.WriteLine(JObject.Parse(jsonPayload));
                            HandleEvent(JObject.Parse(jsonPayload));
                        }
                    }
                } while (true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WS RECEEVE ERROR" + ex.Message);
                Close();
                if (retryCount >= 5)
                {
                    registerAsync();
                }
            }
        }

        private void HandleEvent(JObject payloadObj)
        {
            if (payloadObj.ContainsKey("type") && payloadObj["type"].ToString() == "event")
            {
                if (payloadObj.ContainsKey("event"))
                {
                    JObject eventData = payloadObj["event"].ToObject<JObject>();
                    if (eventData.ContainsKey("message"))
                    {
                        string msg_text = eventData["message"].ToString();
                        string msg_title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                        string msg_image = "";
                        string msg_audio = "";

                        if (eventData.ContainsKey("title"))
                        {
                            msg_title = eventData["title"].ToString();
                        }

                        if (eventData.ContainsKey("data") && eventData["data"].ToObject<JObject>().ContainsKey("image"))
                        {
                            msg_image = eventData["data"].ToObject<JObject>()["image"].ToString();
                        }

                        if (eventData.ContainsKey("data") && eventData["data"].ToObject<JObject>().ContainsKey("audio"))
                        {
                            msg_audio = eventData["data"].ToObject<JObject>()["audio"].ToString();
                        }

                        var app = Application.Current as App;
                        app.ShowNotification(msg_title, msg_text, msg_image, msg_audio);
                    }
                }
            }
        }

        public void Close()
        {
            isSubscribed = false;
            isPingEnabled = false;
            isConnected = false;

            updatePingTimer.Stop();
            socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            Debug.WriteLine("WS closed");
        }
    }
}
