using System;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
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
            Title += (" - " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            AppDomain.CurrentDomain.FirstChanceException += GlobalExceptionFunction;
        }

        static void GlobalExceptionFunction(object source, FirstChanceExceptionEventArgs eventArgs)
        {
            Logger.write("[" + AppDomain.CurrentDomain.FriendlyName + "]" + eventArgs.Exception.ToString(), 3);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            loading.Visibility = Visibility.Visible;
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["url"].Value = url.Text;
            config.AppSettings.Settings["token"].Value = token.Text;
            config.Save(ConfigurationSaveMode.Modified);
            loadingScreenStatus.Content = "savving settings ...";
            Logger.write("settings saved", 1);

            AutoStart.register();
            loadingScreenStatus.Content = "Registering fr autostart...";
            Logger.write("Autostart", 1);

            app.Stop();
            loadingScreenStatus.Content = "Stopping old instances...";
            Logger.write("Stoping Instances", 1);

            if (app.Start())
            {
                loading.Visibility = Visibility.Hidden;
                app.minimalizeToTray();
                return;
            }

            MessageBox.Show("Initialization Failed", "Error");
            loadingScreenStatus.Content = "Initialization Failed!";
            Thread.Sleep(1000);
            loading.Visibility = Visibility.Hidden;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            token.Text = config.AppSettings.Settings["token"].Value;
            url.Text = config.AppSettings.Settings["url"].Value;
            string webhookId = config.AppSettings.Settings["webhookId"].Value;

            if (string.IsNullOrEmpty(webhookId))
            {
                loading.Visibility = Visibility.Hidden;
                Logger.write("Web-hook not found");
                return;
            }
         
            if (!app.Start())
            {
                loading.Visibility = Visibility.Hidden;
                Logger.write("Autostart Failed");
                return;
            }

            loading.Visibility = Visibility.Hidden;
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
