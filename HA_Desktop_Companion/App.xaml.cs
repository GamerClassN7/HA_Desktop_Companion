using System.Threading;
using System.Windows;
using System.Reflection;
using Forms = System.Windows.Forms;
using Windows.UI.Notifications;
using XmlDom = Windows.Data.Xml.Dom;

namespace HA_Desktop_Companion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Forms.NotifyIcon notifyIcon;
        private static Mutex _mutex = null;
        private const string APP_ID = "ToastSample";

        protected override void OnStartup(StartupEventArgs e)
        {
            string appName = Assembly.GetExecutingAssembly().GetName().Name;
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Application.Current.Shutdown();
            }

            notifyIcon =  new Forms.NotifyIcon();
            notifyIcon.Icon = Resource1.ha_logo;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += (s, args) => trayIcon_DoubleClick();

            base.OnActivated(e);
        }

        private void trayIcon_DoubleClick()
        {
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Show();
            MainWindow.Activate();
        }

        public void ShowNotification(string title = "", string body = "", int duration = 20000)
        {

           /* notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
            notifyIcon.BalloonTipText = body;
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.ShowBalloonTip(duration);*/

            string xml = @"<toast>
                          <visual>
                            <binding template=""ToastGeneric"">
                              <image placement=""appLogoOverride"" src=""Resources/MicrosoftLogo.png"" />
                              <text>title</text>
                              <text>body</text>
                            </binding>
                          </visual>
                        </toast>";

            XmlDom.XmlDocument doc = new XmlDom.XmlDocument();
            doc.LoadXml(xml);

            ToastNotification toast = new ToastNotification(doc);
            ToastNotificationManager.CreateToastNotifier("{sadasdd}").Show(toast);

        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose();
            base.OnExit(e);
        }
    }
}
