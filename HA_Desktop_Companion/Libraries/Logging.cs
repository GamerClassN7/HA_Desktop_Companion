using System;
using System.Diagnostics;
using System.IO;

namespace HA_Desktop_Companion.Libraries {
    public class Logging {
        public string logFile;
        private StreamWriter sw;
        public Logging(string logFilePath) {
            sw = new StreamWriter(logFilePath, true);
        }

        public void Write(string msg)
        {
            Debug.WriteLine(msg);
            sw.WriteLine("[" + DateTime.Now.ToString("yyyyMMddTHHmmss") + "]-" + msg);
        }
    }
}
