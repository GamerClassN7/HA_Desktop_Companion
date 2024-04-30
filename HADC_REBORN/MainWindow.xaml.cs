using HADC_REBORN.Class.Helpers;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HADC_REBORN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.ShowInTaskbar = false;
            InitializeComponent();
        }

        private void loadingScreen_MediaEnded(object sender, RoutedEventArgs e)
        {

        }

        private void loadingScreen_Loaded(object sender, RoutedEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            Debug.Write(config.AppSettings.Settings);

            if (!String.IsNullOrEmpty(config.AppSettings.Settings["url"].Value) || !String.IsNullOrEmpty(config.AppSettings.Settings["token"].Value))
            {
                url.Text = config.AppSettings.Settings["url"].Value;
                token.Text = config.AppSettings.Settings["token"].Value;
            }

            loadingScreen.Visibility = Visibility.Hidden;
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void save_MouseClick(object sender, RoutedEventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            
            config.AppSettings.Settings["url"].Value = url.Text;
            config.AppSettings.Settings["token"].Value = token.Text;

            config.Save(ConfigurationSaveMode.Modified);
            App.log.writeLine("Settings Saved");

            System.Windows.MessageBox.Show("SettingsSaved");
        }
    }
}