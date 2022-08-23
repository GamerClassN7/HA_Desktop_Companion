using System;
using System.Diagnostics;
using System.IO;

namespace HA_Desktop_Companion.Libraries {
    public class Logging {
        public string logFile;
        private bool enabled = false;

        public Logging(string logFilePath) {
            if (!File.Exists(logFilePath))
                File.Create(logFilePath);

            logFile = logFilePath;

        }

        public void isEnabled(bool enabled = false)
        {
            enabled = enabled;
        }
        public void Write(string msg)
        {
            if (!enabled)
                return;

            Debug.WriteLine(msg);
            using (StreamWriter sw = File.AppendText(logFile))
            {
                sw.WriteLine("[" + DateTime.Now.ToString("yyyyMMddTHHmmss") + "]-" + msg);
            }
        }
    }
}
