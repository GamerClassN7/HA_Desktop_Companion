using System;
using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace HA_Desktop_Companion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
         private Forms.NotifyIcon notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            notifyIcon =  new Forms.NotifyIcon();
            notifyIcon.Icon = new Icon("ha_logo.ico");
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

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose();
            base.OnExit(e);
        }

    }

}
