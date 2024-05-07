using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Actions
{
    class Notification
    {
        public static void Spawn(string body = "", string title = "", string imageUrl = "", string audioUrl = "", int duration = 500)
        {
            ToastContentBuilder toast = new ToastContentBuilder();
            toast.AddText(body);

            if (!String.IsNullOrEmpty(title))
            {
                toast.AddText(title);
            }

            if (!String.IsNullOrEmpty(imageUrl))
            {
                //TODO: REFACTORING
                string fileName = string.Format("{0}{1}.png", System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
                if (imageUrl.StartsWith("http"))
                {
                    WebClient wc = new WebClient();
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    wc.DownloadFile(imageUrl, fileName);
                }

                toast.AddInlineImage(new Uri("file:///" + fileName));
            }

            if (!String.IsNullOrEmpty(audioUrl))
            {
                //TODO: REFACTORING
                string fileName = string.Format("{0}{1}.wav", System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
                if (audioUrl.StartsWith("http"))
                {
                    WebClient wc = new WebClient();
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    wc.DownloadFile(audioUrl, fileName);
                }

                //playNotificationAudio(fileName, duration);
            }

            toast.Show();
        }
    }
}
