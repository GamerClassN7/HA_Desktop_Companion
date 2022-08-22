using System.Threading;
using System.Windows;
using System.Reflection;
using Forms = System.Windows.Forms;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System;
using System.IO;

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

           /*notifyIcon.BalloonTipIcon = Forms.ToolTipIcon.Info;
            notifyIcon.BalloonTipText = body;
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.ShowBalloonTip(duration);*/

            /*string xml = @"<?xml version=""1.0"" encoding =""utf-8"" ?>
<toast>
	<visual>
		<binding template=""ToastGeneric"">
			<text>Notification Message</text>
			<text>This is a Notification Message</text>
			<image src= ""C:\Users\Admin\Desktop\Toast\toast\Assets\pikachu.png"" />
		</binding>
	</visual>
	<actions>
		<input id=""snoozeTime"" type =""selection"" defaultInput =""15"" >
			<selection id= ""1"" content = ""1 minute"" />
			<selection id= ""15"" content = ""15 minutes"" />
			<selection id= ""60"" content = ""1 hour"" />
			<selection id= ""240"" content = ""4 hours"" />
			<selection id= ""1440"" content = ""1 day"" />
		</input>
		<action
         activationType= ""system""
         arguments = ""snooze""
         hint -inputId= ""snoozeTime""
         content = """"/>
        <action
         activationType= ""system""
         arguments = ""dismiss""
         content = """"/>
    </actions>
</toast>
";

            XmlDom.XmlDocument doc = new XmlDom.XmlDocument();
            doc.LoadXml(xml);*/

            /*XmlDom.XmlDocument doc = new XmlDom.XmlDocument();

            ToastNotification toast = new ToastNotification(doc);
            ToastNotificationManager.CreateToastNotifier(Assembly.GetExecutingAssembly().GetName().Name).Show(toast);*/

            //ToastNotifier ToastNotifier =;

            //Windows.Data.Xml.Dom.XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText04);
            //toastXml.LoadXml(xml);

           // Windows.Data.Xml.Dom.XmlNodeList toastNodeList = toastXml.GetElementsByTagName("text");
           // toastNodeList.Item(0).AppendChild(toastXml.CreateTextNode(title));
          //  toastNodeList.Item(1).AppendChild(toastXml.CreateTextNode(body));

          //  Windows.Data.Xml.Dom.XmlNodeList toastImageNodeList = toastXml.GetElementsByTagName("image");
//Windows.Data.Xml.Dom.XmlElement image = toastXml.CreateElement("image");
            //image.SetAttribute("placement", "appLogoOverride");
         /*   image.SetAttribute("src", "https://upload.wikimedia.org/wikipedia/commons/thumb/c/cb/Flag_of_the_Czech_Republic.svg/96px-Flag_of_the_Czech_Republic.svg.png");
            toastImageNodeList.Item(0).AppendChild(image);
            toastImageNodeList.Item(1).AppendChild(image);



            Windows.Data.Xml.Dom.IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            Windows.Data.Xml.Dom.XmlElement audio = toastXml.CreateElement("audio");
            audio.SetAttribute("src", "ms-winsoundevent:Notification.SMS");

  
            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = DateTime.Now.AddSeconds(4);

            ToastNotificationManager.CreateToastNotifier(Assembly.GetExecutingAssembly().GetName().Name).Show(toast);


            Windows.Data.Xml.Dom.XmlDomImplementation xmlDoc = new Windows.Data.Xml.Dom.XmlDomImplementation();
            MessageBox.Show(doc.GetXml());*/


            /*
             var xml = @"
<toast>
    <visual>
        <binding template="ToastGeneric">
            <text>$($headlineText)</text>
            <text>$($bodyText)</text>
            <image placement="appLogoOverride" src="$($logo)"/>
            <image src=""/>
        </binding>
    </visual>
</toast>
"@
             */
            var message = "Sample message";
            var xml = $"<?xml version=\"1.0\"?><toast><visual><binding template=\"ToastText01\"><text id=\"1\">{message}</text></binding></visual></toast>";
            var toastXml = new XmlDocument();
            toastXml.LoadXml(xml);
            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier(Assembly.GetExecutingAssembly().GetName().Name).Show(toast);


        }

        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose();
            base.OnExit(e);
        }
    }
}
