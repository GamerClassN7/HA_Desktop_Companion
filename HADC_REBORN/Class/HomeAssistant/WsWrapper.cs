using HADC_REBORN.Class.Actions;
using HADC_REBORN.Class.Helpers;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace HADC_REBORN.Class.HomeAssistant.Objects
{
    public class WsWrapper
    {
        private YamlLoader yamlLoader;
        private WsConnector wsConnector;

        private BackgroundWorker wsWorkerRecieverer = new BackgroundWorker();
        private BackgroundWorker wsWorkerPinger = new BackgroundWorker();

        private DispatcherTimer updatePingTimer = new DispatcherTimer();
        private DispatcherTimer reconnectTimer = new DispatcherTimer();
        private volatile bool stopped = false;

        public WsWrapper(YamlLoader yamlLoaderDependency, WsConnector wsConnectorDependency)
        {
            yamlLoader = yamlLoaderDependency;
            wsConnector = wsConnectorDependency;

            // Handlers are attached only once, otherwise every reconnect would multiply them
            wsWorkerRecieverer.DoWork += wsWorkerReciever_DoWork;
            wsWorkerRecieverer.RunWorkerCompleted += wsWorkerReciever_Completed;

            wsWorkerPinger.DoWork += wsWorkePinger_DoWork;

            updatePingTimer.Interval = TimeSpan.FromMinutes(5);
            updatePingTimer.Tick += UpdatePing_Tick;

            reconnectTimer.Interval = TimeSpan.FromSeconds(30);
            reconnectTimer.Tick += Reconnect_Tick;
        }

        public void Connect()
        {
            stopped = false;

            if (!wsConnector.connected())
            {
                if (wsWorkerRecieverer.IsBusy)
                {
                    throw new Exception("Already Registered !!!");
                }

                wsConnector.register();
                wsWorkerRecieverer.RunWorkerAsync();

                updatePingTimer.Start();

                App.log.writeLine("[WS] Ping Initialized");
            }
        }

        private void wsWorkerReciever_Completed(object? sender, RunWorkerCompletedEventArgs e)
        {
            Disconnect();

            if (stopped)
            {
                return;
            }

            App.log.writeLine("[WS] Connection lost, reconnecting in " + reconnectTimer.Interval.TotalSeconds + "s");
            reconnectTimer.Start();
        }

        private async void Reconnect_Tick(object? sender, EventArgs e)
        {
            reconnectTimer.Stop();

            if (stopped || wsConnector.connected())
            {
                return;
            }

            try
            {
                // Connecting blocks until the server answers, keep it off the UI thread
                await Task.Run(Connect);
                App.log.writeLine("[WS] Reconnected");
            }
            catch (Exception ex)
            {
                App.log.writeLine("[WS] Reconnect failed: " + ex.Message);
                wsConnector.disconnect();
                if (!stopped)
                {
                    reconnectTimer.Start();
                }
            }
        }

        // Disconnect for good, e.g. before connecting with new settings
        public void Stop()
        {
            stopped = true;
            reconnectTimer.Stop();
            Disconnect();
        }

        public void Disconnect()
        {
            if (wsConnector.connected())
            {
                App.log.writeLine("[WS] Disconecting");
                wsConnector.disconnect();
                App.log.writeLine("[WS] Disconected");
            }
            if (updatePingTimer.IsEnabled)
            {
                App.log.writeLine("[WS] Ping stoping");
                updatePingTimer.Stop();
                App.log.writeLine("[WS] Ping Stopped");
            }

        }

        private void UpdatePing_Tick(object? sender, EventArgs e)
        {
            if (wsWorkerPinger.IsBusy != true)
            {
                wsWorkerPinger.RunWorkerAsync();
            }
        }

        private void wsWorkePinger_DoWork(object? sender, DoWorkEventArgs e)
        {
            try
            {
                wsConnector.ping();
            }
            catch (Exception ex)
            {
                App.log.writeLine("[WS] Ping Failed: " + ex.Message);
            }
        }

        public static void eventReceive(JObject eventData)
        {
            if (!eventData.ContainsKey("event"))
            {
                return;
            }

            JObject eventPayloadData = eventData["event"].ToObject<JObject>();
            if (eventPayloadData == null)
            {
                return;
            }

            if (eventPayloadData.ContainsKey("message"))
            {
                App.log.writeLine("[WS] Event With Message");

                string msg_text = eventPayloadData["message"].ToString();
                string msg_title = "";
                string msg_image = "";
                string msg_audio = "";

                if (eventPayloadData.ContainsKey("title"))
                {
                    msg_title = eventPayloadData["title"].ToString();
                }

                if (eventPayloadData.ContainsKey("data") && eventPayloadData["data"].ToObject<JObject>().ContainsKey("image"))
                {
                    msg_image = eventPayloadData["data"].ToObject<JObject>()["image"].ToString();
                }

                if (eventPayloadData.ContainsKey("data") && eventPayloadData["data"].ToObject<JObject>().ContainsKey("audio"))
                {
                    msg_audio = eventPayloadData["data"].ToObject<JObject>()["audio"].ToString();
                }

                Notification.Spawn(msg_text, msg_title, msg_image, msg_audio);
            }

            if (eventPayloadData.ContainsKey("data") && eventPayloadData["data"].ToObject<JObject>().ContainsKey("key"))
            {
                Keyboard.SendKey(eventPayloadData["data"].ToObject<JObject>()["key"].ToString());
            }
        }

        private void wsWorkerReciever_DoWork(object? sender, DoWorkEventArgs e)
        {
            try
            {
                wsConnector.receive(eventReceive);
            }
            catch (Exception)
            {

            }
        }

        public void restart()
        {
            Disconnect();
            SpinWait.SpinUntil(() => !wsWorkerRecieverer.IsBusy, TimeSpan.FromSeconds(10));
            Connect();
        }
    }
}
