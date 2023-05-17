using System;
using System.Configuration;
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
            app.Stop();

            if (!app.Start())
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
                if (!app.Start())
                {
                    MessageBox.Show("Autostart Failed", "Error");
                    Logger.write("Autostart Failed");
                }
            }
            else
            {
                Logger.write("Web-hook not found");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            app.ShowNotification("App keeps Running in background!");
            this.ShowInTaskbar = false;
            this.Hide();
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
