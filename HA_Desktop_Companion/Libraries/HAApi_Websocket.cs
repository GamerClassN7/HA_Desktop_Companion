using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json.Nodes;
using System.Threading;
using System.Diagnostics;
using System.Windows;

namespace HA_Desktop_Companion.Libraries
{

    public class HAApi_Websocket
    {
        private static Logging log = new Logging(".\\log.txt");

        private bool wsRegistered = false;

        private string token = "";
        private string webhook_id = "";
        public string base_url = "";
        public string remote_ui_url = "";
        public string cloudhook_url = "";

        private static int interactions = 2;

        public HAApi_Websocket(string baseUrl, string apiToken, string webHookId, string remoteUiUrl = "", string cloudhookUrl = "")
        {
            webhook_id = webHookId;
            token = apiToken;
            base_url = baseUrl;
            remote_ui_url = remoteUiUrl;
            cloudhook_url = cloudhookUrl;

            try
            {
                registerWebSocket();
            }
            catch (Exception)
            {
                var app = Application.Current as App;
                app.ShowNotification(System.Reflection.Assembly.GetExecutingAssembly().GetName().Name, "Unable to connect to Websocket!");
            }
        }

        private async void registerWebSocket()
        {
            using (var socket = new ClientWebSocket())
            try
            {
                await socket.ConnectAsync(new Uri(HAResolveUri() + "/api/websocket"), CancellationToken.None);
                await ReceiveRegistrationRequest(socket, token);
                await SubscribeToNotifications(socket, webhook_id);

                log.Write("WS Registered " + HAResolveUri());
                await Receive(socket);
            }
            catch (Exception ex)
            {
                log.Write("WS ERROR - " + ex.Message);

            }
        }

        static async Task ReceiveRegistrationRequest(ClientWebSocket socket, string token)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);

            WebSocketReceiveResult result;
            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);


                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    string jsonPayload = await reader.ReadToEndAsync();
                    var payload = JsonSerializer.Deserialize<JsonObject>(jsonPayload);
                    if (payload["type"].ToString() == "auth_required")
                    {

                        var body = new
                        {
                            type = "auth",
                            access_token = token
                        };

                        log.Write("WS Auth Request recived " + jsonPayload);

                        await Send(socket, JsonSerializer.Serialize(body));
                        await ReceiveRegistration(socket);
                    }
                }
            }
        }
      
        static async Task ReceiveRegistration(ClientWebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);

            WebSocketReceiveResult result;
            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);


                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    string jsonPayload = await reader.ReadToEndAsync();
                    var payload = JsonSerializer.Deserialize<JsonObject>(jsonPayload);
                    if (payload["type"].ToString() == "auth_ok")
                    {
                        bool wsRegistered = true;
                        log.Write("WS Auth OK " + jsonPayload);
                    }
                }
            }
        }

        static async Task ReceiveSubscribeNotificationsRequest(ClientWebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[2048]);

            WebSocketReceiveResult result;
            using (var ms = new MemoryStream())
            {
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);


                using (var reader = new StreamReader(ms, Encoding.UTF8))
                {
                    string jsonPayload = await reader.ReadToEndAsync();
                    var payload = JsonSerializer.Deserialize<JsonObject>(jsonPayload);
                    log.Write("WS NOTIFICATIONS JOINED " + jsonPayload);
                }
            }
        }

        static async Task Receive(ClientWebSocket socket)
        {
            try
            {
                log.Write("WS RECEEVE LOOP STARTED");

                var buffer = new ArraySegment<byte>(new byte[2048]);
                do
                {
                    WebSocketReceiveResult result;
                    using (var ms = new MemoryStream())
                    {
                        do
                        {
                            result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                            ms.Write(buffer.Array, buffer.Offset, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                            break;

                        ms.Seek(0, SeekOrigin.Begin);
                        using (var reader = new StreamReader(ms, Encoding.UTF8))
                        {
                            string jsonPayload = await reader.ReadToEndAsync();

                            var payload = JsonSerializer.Deserialize<JsonObject>(jsonPayload);
                            log.Write("WS PAYLOAD " + payload);

                            if (payload["type"].ToString() == "event")
                            {
                                if (payload["event"].AsObject().ContainsKey("message"))
                                {
                                    string msg_text = payload["event"].AsObject()["message"].ToString();
                                    string msg_title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

                                    if (payload["event"].AsObject().ContainsKey("title"))
                                    {
                                        msg_title = payload["event"].AsObject()["title"].ToString();
                                    }
                                  
                                    var app = Application.Current as App;
                                    app.ShowNotification(msg_title, msg_text, 300000);
                                }
                            }
                        }
                    }
                } while (true);
            }
            catch (Exception)
            {

                throw;
            }
        }

        static async Task SubscribeToNotifications(ClientWebSocket socket, string webhook_id)
        {
            log.Write("WS NOTIFICATIONS Request Send");

            var body = new
            {
                id = interactions,
                type = "mobile_app/push_notification_channel",
                webhook_id = webhook_id,
            };

            await Send(socket, JsonSerializer.Serialize(body));
            await ReceiveSubscribeNotificationsRequest(socket);
        }

        static async Task Send(ClientWebSocket socket, string data)
        {
            interactions = interactions + 1;
            await socket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private string HAResolveUri()
        {
            string resultUrl = base_url;

            if (resultUrl.StartsWith("https://"))
                resultUrl = resultUrl.Replace("https://", "ws://");
            else if (resultUrl.StartsWith("http://"))
                resultUrl = resultUrl.Replace("http://", "ws://");

            if (!string.IsNullOrEmpty(remote_ui_url))
                resultUrl = remote_ui_url;

            if (resultUrl.ToString().EndsWith("/"))
                resultUrl = resultUrl.Substring(0, resultUrl.Length - 1);

            return resultUrl;
        }
    }
}
