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
        public WsWrapper(YamlLoader yamlLoaderDependency, WsConnector wsConnectorDependency)
        {
            yamlLoader = yamlLoaderDependency;
            wsConnector = wsConnectorDependency;
        }

        public void Connect()
        {

            if (!wsConnector.connected())
            {
                wsConnector.register();
                if (wsWorkerRecieverer.IsBusy)
                {
                    throw new Exception("Already Registered !!!");
                }

                wsWorkerRecieverer.DoWork += wsWorkerReciever_DoWork;
                wsWorkerRecieverer.RunWorkerCompleted += wsWorkerReciever_Completed;

                wsWorkerRecieverer.RunWorkerAsync();

                wsWorkerPinger.DoWork += wsWorkePinger_DoWork;

                updatePingTimer.Interval = TimeSpan.FromMinutes(5);
                updatePingTimer.Tick += UpdatePing_Tick;
                updatePingTimer.Start();

                App.log.writeLine("[WS] Ping Initialized");
            }
        }

        private void wsWorkerReciever_Completed(object? sender, RunWorkerCompletedEventArgs e)
        {
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
                Keyboard.SendKey(eventData["data"].ToObject<JObject>()["key"].ToString());
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
            do
            {
                Disconnect();
            } while (wsWorkerRecieverer.IsBusy);
            Connect();
        }
    }
}
