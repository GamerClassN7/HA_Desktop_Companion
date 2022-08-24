using System;
using System.Diagnostics;
using System.IO;

namespace HA_Desktop_Companion.Libraries {
    public class Logging {
        public string logFile;
        private StreamWriter sw;
        public Logging(string logFilePath) {
            if (!File.Exists(logFilePath))
                File.Create(logFilePath);

            logFile = logFilePath;
            sw = File.AppendText(logFile);
        }
            

        public void Write(string msg)
        {
            Debug.WriteLine(msg);
            sw.WriteLine("[" + DateTime.Now.ToString("yyyyMMddTHHmmss") + "]-" + msg);
        }
    }
}
