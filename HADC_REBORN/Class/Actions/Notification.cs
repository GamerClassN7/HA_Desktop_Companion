using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
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

            toast.Show();
        }
    }
}
