using System;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json.Nodes;
using System.Threading;
using System.Windows;

namespace HA_Desktop_Companion.Libraries
{

    public class HAApi_Websocket
    {
        private static Logging log = new Logging(".\\log.txt");

        private bool wsRegistered = false;
        private bool wsNotificationsSubscribed = false;

        private string token = "";
        private string webhook_id = "";
        public string base_url = "";
        public string remote_ui_url = "";
        public string cloudhook_url = "";

        private static int interactions = 2;
        private ClientWebSocket socket = new ClientWebSocket();

        public HAApi_Websocket(string baseUrl, string apiToken, string webHookId, string remoteUiUrl = "", string cloudhookUrl = "")
        {
            webhook_id = webHookId;
            token = apiToken;
            base_url = baseUrl;
            remote_ui_url = remoteUiUrl;
            cloudhook_url = cloudhookUrl;

            registerWebSocket();
        }

        private void registerWebSocket()
        {
            socket.Options.KeepAliveInterval = TimeSpan.Zero;
            socket.ConnectAsync(new Uri(HAResolveUri() + "/api/websocket"), CancellationToken.None).Wait();

            /*AUTHENTICATION PHASE*/
            JsonObject payload = recieveJsonObjectFromWS();
            if (payload["type"].ToString() != "auth_required")
            {
                return;
            }

            var body = new
            {
                type = "auth",
                access_token = token
            };
            log.Write("WS -> " + JsonSerializer.Serialize(body));
            socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body))), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

            payload = recieveJsonObjectFromWS();
            if (payload["type"].ToString() != "auth_ok")
            {
                return;
            }

            wsRegistered = true;
            log.Write("WS Auth OK ");

            /*NOTIFICATION SUBSCRIPTION PHASE*/
            var notificationRegReqBody = new
            {
                id = interactions,
                type = "mobile_app/push_notification_channel",
                webhook_id = webhook_id,
            };

            socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notificationRegReqBody))), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
            log.Write("WS -> " + JsonSerializer.Serialize(notificationRegReqBody));


            payload = recieveJsonObjectFromWS();
            if (payload["success"].ToString() != "true")
            {
                return;
            }

            wsNotificationsSubscribed = true;
            log.Write("WS not sub OK ");

            /*WEBSOCKET RECIEVE PHASE*/
            Receive(socket);
        }

        private JsonObject recieveJsonObjectFromWS()
        {
            byte[] buffer = new byte[2048];
            var result = socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;
            string stringPayload = Encoding.UTF8.GetString(buffer, 0, result.Count);
            log.Write("WS <- " + stringPayload);
            return JsonSerializer.Deserialize<JsonObject>(stringPayload);
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
                            log.Write("WS <- " + payload);

                            if (payload["type"].ToString() == "event")
                            {
                                if (payload["event"].AsObject().ContainsKey("message"))
                                {
                                    string msg_text = payload["event"].AsObject()["message"].ToString();
                                    string msg_title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
                                    string msg_image = "";


                                    if (payload["event"].AsObject().ContainsKey("title"))
                                    {
                                        msg_title = payload["event"].AsObject()["title"].ToString();
                                    }

                                    if (payload["event"].AsObject().ContainsKey("data") && payload["event"].AsObject()["data"].AsObject().ContainsKey("image"))
                                    {
                                        msg_image = payload["event"].AsObject()["data"].AsObject()["image"].ToString();
                                    }

                                    var app = Application.Current as App;
                                    app.ShowNotification(msg_title, msg_text, msg_image, 300000);
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

        public void Check()
        {
            if (socket.State != WebSocketState.Open)
            {
                log.Write("WS Closed Reregistering");
                registerWebSocket();
            }
        }
    }
}
