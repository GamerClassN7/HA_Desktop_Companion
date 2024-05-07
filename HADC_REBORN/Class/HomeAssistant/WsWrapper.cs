using HADC_REBORN.Class.Actions;
using HADC_REBORN.Class.Helpers;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

            if (!wsConnector.isConnected)
            {
                wsConnector.register();
                if (wsWorkerRecieverer.IsBusy)
                {
                    throw new Exception("Already Registered !!!");
                }

                wsWorkerRecieverer.DoWork += wsWorkerReciever_DoWork;
                wsWorkerRecieverer.RunWorkerAsync();

                wsWorkerPinger.DoWork += wsWorkePinger_DoWork;
                
                updatePingTimer.Interval = TimeSpan.FromMinutes(5);
                updatePingTimer.Tick += UpdatePing_Tick;
                updatePingTimer.Start();

                App.log.writeLine("[WS] Ping Initialized");
            }
        }

        public void Disconnect()
        {
            wsConnector.disconnect();
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

        private void wsWorkerReciever_DoWork(object? sender, DoWorkEventArgs e)
        {
            wsConnector.receive();
        }

        private void restart()
        {
            do
            {
                Disconnect();
                Thread.Sleep(1000);
            } while (wsWorkerRecieverer.IsBusy);
            Connect();
        }

    }
}
