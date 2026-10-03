using HADC_REBORN.Class.HomeAssistant.Objects;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace HADC_REBORN.Class.HomeAssistant
{
    public class WsConnector
    {
        private string url;
        private string token;
        private string webhook;

        private ClientWebSocket socket = new ClientWebSocket();
        private byte[] buffer = new byte[2048];
        private static readonly TimeSpan handshakeTimeout = TimeSpan.FromSeconds(30);
        private int interactions = 1;

        private int failedAttempts = 0;
        private bool isConnected = false;

        private Task recieveLoopObject;
        private static DispatcherTimer updatePingTimer = new DispatcherTimer();
        private int notification_event_subscribe_id = 0;

        public WsConnector(string apiUrl, string apiToken, string webhookId)
        {
            url = apiUrl.TrimEnd('/');
            token = apiToken;
            webhook = webhookId;
        }

        public void register()
        {
            Uri wsAddress = new Uri(url + "/api/websocket");
            isConnected = false;
            socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = TimeSpan.Zero;

            using (CancellationTokenSource timeout = new CancellationTokenSource(handshakeTimeout))
            {
                socket.ConnectAsync(wsAddress, timeout.Token).GetAwaiter().GetResult();
            }
            App.log.writeLine("[WS] ADDRESS:" + wsAddress);

            JObject initialization = RecieveAsync();
            if (initialization["type"].ToString() != "auth_required")
            {
                throw new Exception("Server dont asked for authorization");
            }

            WsAuth authObj = new WsAuth() { };
            authObj.access_token = token;

            JObject registration = sendAndRecieveAsync(authObj);
            if (registration["type"].ToString() != "auth_ok")
            {
                throw new Exception("Server dont accepted authorization");
            }

            WsRequest subscribeObj = new WsRequest { };
            subscribeObj.id = interactions;
            subscribeObj.webhook_id = webhook;
            subscribeObj.type = "mobile_app/push_notification_channel";
            
            notification_event_subscribe_id = interactions;
            
            JObject subscription = sendAndRecieveAsync(subscribeObj);
            if (bool.Parse(subscription["success"].ToString()) != true)
            {
                throw new Exception("Server authorization failed");
            }

            isConnected = true;
        }

        public void receive(Action<JObject> callbackRecieveEvent)
        {
            var localBuffer = new ArraySegment<byte>(new byte[2048]);
            bool error = false;

            do
            {
                try
                {
                    WebSocketReceiveResult localResult;
                    using (var ms = new MemoryStream())
                    {
                        do
                        {
                            Task<WebSocketReceiveResult> receiveTask = socket.ReceiveAsync(localBuffer, CancellationToken.None);
                            receiveTask.Wait();

                            localResult = receiveTask.Result;
                            ms.Write(localBuffer.Array, localBuffer.Offset, localResult.Count);
                        } while (!localResult.EndOfMessage);

                        if (localResult.MessageType == WebSocketMessageType.Close)
                            break;

                        ms.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            string jsonPayload = reader.ReadToEnd();
                            if (callbackRecieveEvent != null)
                            {
                                callbackRecieveEvent.Invoke(JObject.Parse(jsonPayload));
                            }
                            App.log.writeLine("[WS] Recieved in LOOP: " + jsonPayload);
                        }
                    }
                }
                catch (Exception e)
                {
                    App.log.writeLine("[WS] ERROR: " + e.Message);
                    error = true;
                }
            } while (!error);
        }

        public void ping()
        {
            WsPing pingObj = new WsPing { };
            pingObj.type = "ping";
            pingObj.id = interactions;
            Send(pingObj);
        }
      
        public void disconnect()
        {
            isConnected = false;

            if (socket == null)
            {
                return;
            }

            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    //https://developers.home-assistant.io/docs/api/websocket#unsubscribing-from-events
                    // Send only, the response is consumed by the running receive loop
                    WsUnsubscribeRequest unsubscribeObj = new WsUnsubscribeRequest { };
                    unsubscribeObj.id = interactions;
                    unsubscribeObj.type = "unsubscribe_events";
                    unsubscribeObj.subscription = notification_event_subscribe_id;
                    Send(unsubscribeObj).Wait(TimeSpan.FromSeconds(2));
                }
            }
            catch (Exception e)
            {
                App.log.writeLine("[WS] Unsubscribe failed: " + e.Message);
            }

            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None).Wait(TimeSpan.FromSeconds(2));
                }
                else
                {
                    socket.Abort();
                }
            }
            catch (Exception e)
            {
                App.log.writeLine("[WS] Close failed: " + e.Message);
            }

            socket.Dispose();
            socket = null;
        }

        public bool connected()
        {
            return (isConnected && (failedAttempts < 5) && socket != null);
        }

        private JObject sendAndRecieveAsync(dynamic payloadObj)
        {
            Send(payloadObj).Wait();

            return RecieveAsync();
        }

        private JObject RecieveAsync()
        {
            // Messages can be split into multiple frames, read until the end of the message
            using CancellationTokenSource timeout = new CancellationTokenSource(handshakeTimeout);
            using MemoryStream ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token).GetAwaiter().GetResult();
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new Exception("Server closed the connection");
                }
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            string JSONRecievedpayload = Encoding.UTF8.GetString(ms.ToArray());

            App.log.writeLine("[WS] RECIEVED:");
            App.log.writeLine(JSONRecievedpayload);

            return JObject.Parse(JSONRecievedpayload);
        }
       
        private async Task Send(dynamic payloadObj)
        {
            string JSONPayload = JsonConvert.SerializeObject(payloadObj, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }).ToString();

            App.log.writeLine("[WS] SEND:");
            App.log.writeLine(JSONPayload);

            ArraySegment<byte> BYTEPayload = new ArraySegment<byte>(Encoding.UTF8.GetBytes(JSONPayload));
            socket.SendAsync(BYTEPayload, WebSocketMessageType.Text, true, CancellationToken.None).Wait();

            interactions = interactions + 1;
        }
    }
}
