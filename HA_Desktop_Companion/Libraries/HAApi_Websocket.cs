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
  
        private bool wsRegistered = false;
        private string token = "";
        private string webhook_id = "";
        private static int interactions = 2;

        public HAApi_Websocket(string apiToken, string webHookId)
        {
            webhook_id = webHookId;
            token = apiToken;
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
                await socket.ConnectAsync(new Uri("ws://home.jonatanrek.cz/api/websocket"), CancellationToken.None);
                await ReceiveRegistrationRequest(socket, token);
                await SubscribeToNotifications(socket, webhook_id);

                Debug.WriteLine("WS Registered");

                await Receive(socket);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WS ERROR - " + ex.Message);
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
                        Debug.WriteLine("WS Auth Request recived " + jsonPayload);

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
                        Debug.WriteLine("WS Auth OK " + jsonPayload);
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
                    Debug.WriteLine("WS NOTIFICATIONS JOINED " + jsonPayload);
                }
            }
        }

        static async Task Receive(ClientWebSocket socket)
        {
            try
            {
                Debug.WriteLine("WS RECEEVE LOOP STARTED");

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
                            if (payload["type"].ToString() == "event")
                            {
                                if (payload["event"].AsObject().ContainsKey("message"))
                                {
                                    string msg_text = payload["event"].AsObject()["message"].ToString();
                                    string msg_title = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

                                    if (payload["event"].AsObject().ContainsKey("title"))
                                    {
                                        msg_title = payload["event"].AsObject().ContainsKey("title").ToString();
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
            Debug.WriteLine("WS NOTIFICATIONS Request Send");
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
    }
}
