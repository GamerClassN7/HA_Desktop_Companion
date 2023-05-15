using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using static System.Net.Mime.MediaTypeNames;

namespace HA.Class.Helpers
{
    public class Logger
    {
        private static bool initialized = false;
        private static string path1;


        private static void init(string path = "./log.log")
        {
            path1 = Path.Combine(path).ToString();
            if (File.Exists(path1))
            {
                File.WriteAllText(path1, getMessage("Initialization"));
            }
            initialized = true;
        }

        /*
            0 - Info
            1 - warning
            3 - error
            4 - seecrets
         */

        public static void write(string msg, int level = 0)
        {
            if (!initialized)
            {
                init();
            }

            Debug.WriteLine(msg);
            File.AppendAllText(path1, getMessage(msg));
        }

        public static void write(object msg, int level = 0)
        {
            if (!initialized)
            {
                init();
            }

            string msg_str = msg.ToString();
            Debug.WriteLine(msg_str);
            File.AppendAllText(path1, getMessage(msg_str));

        }
        
        private static string getMessage(string text)
        {
            return DateTime.Now.ToString("[MM/dd/yyyy-HH:mm:ss]") + text + "\n";
        }
    }
}
