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
        private string appDir = AppDomain.CurrentDomain.BaseDirectory;
#endif
        private string logFilePath;
        private DateTime lastInitializeDateTime;
        private static string[] secreetsStrings = new string[] { };

        public Logger() {
            logFilePath = initialize();
        }

        public void setSecreets(string[] strings)
        {
            if (!isInistialized)
            {
                logFilePath = initialize();
            }
            secreetsStrings = strings.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

        public string getLogPath()
        {
            return logFilePath;
        }

        private string initialize()
        {
            string logFolderPath = Path.Combine(appDir, "logs");
            Debug.WriteLine(logFolderPath);

            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            string logFileName = "log_" + ((DateTime.Now).ToString("MM_dd_yyyy")) + ".log";
            string logFilePath = Path.Combine(logFolderPath, logFileName);

            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, getLogMessage("Initializing",0), System.Text.Encoding.UTF8);
            }

            removeOldLogFiles(logFolderPath);

            lastInitializeDateTime = DateTime.Now;
            isInistialized = true;

            return logFilePath;
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
            string parsedText = text;
            if (secreetsStrings.Length > 0)
            {
                foreach (string secret in secreetsStrings)
                {
                    parsedText = parsedText.Replace(secret, "***SECRET****");
                }
            }

            return DateTime.Now.ToString("[MM/dd/yyyy-HH:mm.ss]") + "[" + type + "]" + parsedText + "\n";
        }

        public void writeLine(string msg, int type = 0)
        {
            int InitilizedBeforeNumberOfDays = (int)(DateTime.Now - lastInitializeDateTime).TotalDays;
            if (!isInistialized || InitilizedBeforeNumberOfDays > 0)
            {
                logFilePath = initialize();
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
