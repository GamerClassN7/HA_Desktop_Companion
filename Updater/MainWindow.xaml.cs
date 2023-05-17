﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Windows;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private App app;
        public MainWindow()
        {
            app = Application.Current as App;
            InitializeComponent();
        }

        private void Window_Activated(object sender, EventArgs e)
        {
          
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(app.appDir + "/cache/"))
                Directory.CreateDirectory(app.appDir + "/cache/");

            using (var client = new WebClient())
            {
                client.DownloadFile(app.zipUrl, app.appDir + "/cache/" + app.versionString + "_" + app.zipName);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (app.isLatest)
            {
                Environment.Exit(0);
            }
        }
    }
}
