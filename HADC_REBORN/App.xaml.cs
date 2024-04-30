using System.Configuration;
using System.Data;
using System.Windows;

using From = System.Windows.Forms;

namespace HADC_REBORN
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static NotifyIcon icon;

        protected override void OnStartup(StartupEventArgs e)
        {
            App.icon = new NotifyIcon();

            icon.Click += new EventHandler(icon_click);
            icon.Icon = HADC_REBORN.Resource.ha_icon;
            icon.Text = System.AppDomain.CurrentDomain.FriendlyName;
            icon.Visible = true;

            icon.ContextMenuStrip = new ContextMenuStrip();
            icon.ContextMenuStrip.Items.Add("Home Assistant", null, null);
            icon.ContextMenuStrip.Items.Add("Log", null, null);
            icon.ContextMenuStrip.Items.Add("Send Test Notification", null, null);
            icon.ContextMenuStrip.Items.Add("Quit", null, null);

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            icon.Dispose();

            base.OnExit(e);
        }

        private void icon_click(Object sender, EventArgs e)
        {

        }
    }

}
