using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Helpers
{
    public class Logger
    {
        private static bool isInistialized = false;
#if DEBUG
        private string appDir = Directory.GetCurrentDirectory();
#else
        private string appDir = AppDomain.CurrentDomain.BaseDirectory();
#endif
        private string logFilePath;
        private DateTime lastInitializeDateTime;

        public Logger() {
            initialize();
        }

        public string getLogPath()
        {
            return logFilePath;
        }

        private void initialize()
        {
            string logFolderPath = Path.Combine(appDir, "logs");
            Debug.WriteLine(logFolderPath);

            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            string logFileName = "log_" + ((DateTime.Now).ToString("MM_dd_yyyy")) + ".log";
            logFilePath = Path.Combine(logFolderPath, logFileName);
            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, getLogMessage("Initializing",0), System.Text.Encoding.UTF8);
            }
            removeOldLogFiles(logFolderPath);

            lastInitializeDateTime = DateTime.Now;
            isInistialized = true;
        }

        private void removeOldLogFiles(string rootLogFolderPath, int daysBack = -3)
        {
            string logFileName = "log_"+((DateTime.Now).AddDays(daysBack).ToString("MM_dd_yyyy"))+".log";
            string LogFileToDelete = Path.Combine(rootLogFolderPath, logFileName);
            if (File.Exists(LogFileToDelete))
            {
                File.Delete(LogFileToDelete);
            }
        }

        private string getLogMessage(string text, int type = 0)
        {
            return DateTime.Now.ToString("[MM/dd/yyyy-HH:mm.ss]") + "[" + type + "]" + text + "\n";
        }

        public void writeLine(string msg, int type = 0)
        {
            int InitilizedBeforeNumberOfDays = (int)(DateTime.Now - lastInitializeDateTime).TotalDays;
            if (!isInistialized || InitilizedBeforeNumberOfDays >= 1)
            {
                initialize();
            }

            Debug.WriteLine(msg);

            FileStream fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter streamWriter = new StreamWriter(fileStream);
            streamWriter.Write(getLogMessage(msg, type));
            streamWriter.Dispose();
            fileStream.Dispose();
        }
    }
}
