using HADC_REBORN.Class.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Management;
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
        private BackgroundWorker wsWorkerRecieverer;
        private DispatcherTimer updatePingTimer = new DispatcherTimer();
        public WsWrapper(YamlLoader yamlLoaderDependency, WsConnector wsConnectorDependency)
        {
            yamlLoader = yamlLoaderDependency;
            wsConnector = wsConnectorDependency;
        }

        public void Connect()
        {
            wsWorkerRecieverer = new BackgroundWorker();

            if (!wsConnector.isConnected)
            {
                wsConnector.register();
                wsWorkerRecieverer.DoWork += wsWorkerReciever_DoWork;
                if (wsWorkerRecieverer.IsBusy)
                {
                    throw new Exception("Already Registered !!!");
                }

                wsWorkerRecieverer.RunWorkerAsync();
                updatePingTimer = new DispatcherTimer();
                updatePingTimer.Interval = TimeSpan.FromMinutes(30);
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
            wsConnector.ping();
        }

        private void wsWorkerReciever_DoWork(object? sender, DoWorkEventArgs e)
        {
            try
            {
                wsConnector.receive();
            }
            catch (Exception)
            {
                restart();
            }
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
