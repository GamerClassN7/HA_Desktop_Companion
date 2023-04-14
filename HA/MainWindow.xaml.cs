using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HA.Class;

namespace HA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            foreach (var key in config.AppSettings.Settings.AllKeys)
            {
                config.AppSettings.Settings[key].Value = "";       
            }

            config.AppSettings.Settings["url"].Value = url.Text;
            config.AppSettings.Settings["token"].Value = token.Text;

            config.Save(ConfigurationSaveMode.Modified);

            App.Stop();
            if (!App.Start())
            {
                MessageBox.Show("Initialization Failed", "Error");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Process.Start(".\\Updater.exe", "https://api.github.com/repos/GamerClassN7/HA_Desktop_Companion/releases 0.0.1");

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            token.Text = config.AppSettings.Settings["token"].Value;
            url.Text = config.AppSettings.Settings["url"].Value;
            string webhookId = config.AppSettings.Settings["webhookId"].Value;

            if (!string.IsNullOrEmpty(webhookId))
            {
                if (!App.Start())
                {
                    MessageBox.Show("Autoregistration Failed", "Error");
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var app = Application.Current as App;
            app.ShowNotification(Assembly.GetExecutingAssembly().GetName().Name, "App keeps Running in background!");

            //this.ShowInTaskbar = false;
            //this.Hide();

            //e.Cancel = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            App.Close();
        }
    }
}
