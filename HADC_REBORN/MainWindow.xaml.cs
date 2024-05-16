using HADC_REBORN.Class.Actions;
using HADC_REBORN.Class.Helpers;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HADC_REBORN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private App app;
        private DispatcherTimer statusTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
            app = (App)App.Current;

            statusTimer.Interval = TimeSpan.FromSeconds(5);
            statusTimer.Tick += statusTimer_Tick;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var isLightTheme = Theme.isLightTheme();
            HwndSource source = (HwndSource)PresentationSource.FromVisual(this);
            Theme.UseImmersiveDarkMode(source.Handle, !isLightTheme);
            Theme.setTheme(isLightTheme);
        }

        
        private void loadingScreen_Loaded(object sender, RoutedEventArgs e)
        {
            this.ShowInTaskbar = true;

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            App.log.writeLine("Loading Setting File");

            if (!String.IsNullOrEmpty(config.AppSettings.Settings["url"].Value) || !String.IsNullOrEmpty(config.AppSettings.Settings["token"].Value))
            {
                App.log.writeLine("Previouse settings found");
                url.Text = config.AppSettings.Settings["url"].Value;
                token.Text = config.AppSettings.Settings["token"].Value;
            }

            App.log.writeLine("Initial Loading Done");
            loadingScreen.Visibility = Visibility.Hidden;

            if (app.initializing == true && app.Start())
            {
                Close();
            }

            app.initializing = false;
        }

        private void statusTimer_Tick(object? sender, EventArgs e)
        {
            updateStatus();
        }

        private void save_MouseClick(object sender, RoutedEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["url"].Value = url.Text.TrimEnd('/');
            config.AppSettings.Settings["token"].Value = token.Text;

            config.AppSettings.Settings["remote_url"].Value = "";
            config.AppSettings.Settings["cloud_url"].Value = "";
            config.AppSettings.Settings["webhook_id"].Value = "";
            config.AppSettings.Settings["secret"].Value = "";

            config.Save(ConfigurationSaveMode.Modified);
            App.log.writeLine("Settings Saved");
            Notification.Spawn("Settings Saved");

            app.Stop();
            if (app.Start())
            {
                Close();
            }
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            App.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.ShowInTaskbar = false;
            Notification.Spawn("App keeps Running in background!");
            statusTimer.Stop();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title += (" - " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            updateStatus();
            statusTimer.Start();
        }

        private void updateStatus()
        {
            if (app.haApiConnector != null && app.haApiConnector.connected() == true)
            {
                api_status.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                api_status.Foreground = new SolidColorBrush(Colors.Red);
            }

            if (app.haWsConnector != null && app.haWsConnector.connected() == true)
            {
                ws_status.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                ws_status.Foreground = new SolidColorBrush(Colors.Red);
            }
        }
    }
}