using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Windows.Media.Protection.PlayReady;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace GH_Updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly HttpClient client = new HttpClient();

        private static string appDir = Directory.GetCurrentDirectory();

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow wnd = new MainWindow();
             if (e.Args.Length == 2)
             {
                wnd.checkForUpdates(e.Args[0], e.Args[1]);
                wnd.Show();
             } else
             {
                Environment.Exit(0);
            }
        }
    }
}
