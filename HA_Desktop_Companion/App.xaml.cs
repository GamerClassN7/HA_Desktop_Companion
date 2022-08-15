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
        Forms.NotifyIcon trayIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            trayIcon =  new Forms.NotifyIcon();
            trayIcon.Icon = new Icon("ha_logo.ico");
            trayIcon.Visible = true;
            trayIcon.Click += new System.EventHandler(trayIcon_DoubleClick);

            base.OnActivated(e);
        }

        private void trayIcon_DoubleClick(object? sender, EventArgs e)
        {
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            trayIcon.Dispose();
            base.OnExit(e);
        }
    }

}
