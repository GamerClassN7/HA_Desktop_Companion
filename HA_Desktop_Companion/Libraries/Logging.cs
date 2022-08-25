﻿using System;
using System.Diagnostics;
using System.IO;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace HA_Desktop_Companion.Libraries {
    public class Logging {
        public string logFile;
        private StreamWriter sw;
        
        public Logging(string logFilePath) {
            FileStream fs = File.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
        }

        public void Write(string msg, bool logOnly = false)
        {
            if (logOnly)
                Debug.WriteLine(msg);

            sw.WriteLine("[" + DateTime.Now.ToString("yyyyMMddTHHmmss") + "]-" + msg);
        }
    }
}
