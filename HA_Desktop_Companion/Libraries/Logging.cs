using System;
using System.Diagnostics;
using System.IO;

namespace HA_Desktop_Companion.Libraries {
    public class Logging {
        public string logFile;

        public Logging(string logFilePath) {
            if (!File.Exists(logFilePath))
                File.Create(logFilePath);

            logFile = logFilePath;

        }

        public void Write(string msg)
        {
            Debug.WriteLine(msg);
            using (StreamWriter sw = File.AppendText(logFile))
            {
                sw.WriteLine("[" + DateTime.Now.ToString("yyyyMMddTHHmmss") + "]-" + msg);
            }
        }
    }
}
