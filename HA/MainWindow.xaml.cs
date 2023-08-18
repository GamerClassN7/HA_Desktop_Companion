using System;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
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
            loading_Show();
            //new Task(settings_Save).Start();  
            settings_Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loading_Show();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            token.Text = config.AppSettings.Settings["token"].Value;
            url.Text = config.AppSettings.Settings["url"].Value;
            string webhookId = config.AppSettings.Settings["webhookId"].Value;

            if (string.IsNullOrEmpty(webhookId))
            {
                //loading_Hide();
                Logger.write("Web-hook not found");
                return;
            }
         
            if (!app.Start())
            {
                //loading_Hide();
                Logger.write("Autostart Failed");
                return;
            }

            loading_Hide();
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

        public void loading_Show()
        {
            loading.Visibility = Visibility.Visible;
            Thread.Sleep(2500);
        }

        public void loading_Hide()
        {
            Thread.Sleep(2500);
            loading.Visibility = Visibility.Hidden;
        }

        public void settings_Save()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["url"].Value = url.Text;

            if (config.AppSettings.Settings["token"].Value != token.Text)
            {
                Logger.write("Saving Difrent Token", 1);
                config.AppSettings.Settings["token"].Value = token.Text;
            }

            config.AppSettings.Settings["webhookId"].Value = "";
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
                loading_Hide();
                app.minimalizeToTray();
                return;
            }

            MessageBox.Show("Initialization Failed", "Error");
            loadingScreenStatus.Content = "Initialization Failed!";
            loading_Hide();
        }
    }
}
