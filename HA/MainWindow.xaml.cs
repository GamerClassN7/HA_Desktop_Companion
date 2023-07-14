using System;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using HA.Class.Helpers;

namespace HA
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
            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
            {
                Logger.write("["+ AppDomain.CurrentDomain.FriendlyName + "]" + eventArgs.Exception.ToString(), 1);
            };

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

            AutoStart.register();
            Logger.write("Autostart", 1);

            app.Stop();
            Logger.write("Stoping Instances", 1);

            if (app.Start())
            {
                app.minimalizeToTray();
                return;
            }
            
            MessageBox.Show("Initialization Failed", "Error");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //string updaterPath = (app.GetRootDir() + "\\Updater.exe");
            //if (System.IO.File.Exists(updaterPath)) {
            //    Process.Start(updaterPath, "https://api.github.com/repos/GamerClassN7/HA_Desktop_Companion/releases 0.0.0");
            //} else {
           //     Logger.write("Updater not found",1);
            //}

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            token.Text = config.AppSettings.Settings["token"].Value;
            url.Text = config.AppSettings.Settings["url"].Value;
            string webhookId = config.AppSettings.Settings["webhookId"].Value;

            if (string.IsNullOrEmpty(webhookId))
            {
                MessageBox.Show("Web-hook");

                Logger.write("Web-hook not found");
                return;
            }
         
            if (!app.Start())
            {
                MessageBox.Show("Autostart Failed", "Error");
                Logger.write("Autostart Failed");
                return;
            }

            app.minimalizeToTray(false);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            app.minimalizeToTray();
            e.Cancel = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            app.Close();
        }

        private void token_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            
        }
    }
}
