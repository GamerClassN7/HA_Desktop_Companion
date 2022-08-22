using System.Threading;
using System.Windows;
using System.Reflection;
using Forms = System.Windows.Forms;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System;
using System.Net;

using System.IO;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;

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

        public void ShowNotification(string title = "", string body = "", string imageUrl = "",  int duration = 20000)
        {
            var toast = new ToastContentBuilder();

            if (!String.IsNullOrEmpty(title))
            {
                toast.AddText(title);
            }

            if (!String.IsNullOrEmpty(imageUrl))
            {
                if (imageUrl.StartsWith("http"))
                {
                    Debug.WriteLine(imageUrl);
                    new WebClient().DownloadFile(new Uri(imageUrl), System.IO.Path.GetTempPath() + "not_img.png");
                }
                toast.AddInlineImage(new Uri("file:///"+ System.IO.Path.GetTempPath() + "not_img.png"));
                Debug.WriteLine("file:///" + System.IO.Path.GetTempPath() + "not_img.png");

            }

            toast.AddText(body);
            toast.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose();
            base.OnExit(e);
        }
    }
}
