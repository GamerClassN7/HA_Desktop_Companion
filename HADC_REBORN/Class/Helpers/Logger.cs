using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace HADC_REBORN.Class.Helpers
{
    public class Logger
    {
        private const int keepLogDays = 3;
        private static readonly object writeLock = new object();

#if DEBUG
        private string appDir = Directory.GetCurrentDirectory();
#else
        private string appDir = AppDomain.CurrentDomain.BaseDirectory;
#endif
        private string logFilePath;
        private DateTime logFileDate;
        private static string[] secreetsStrings = new string[] { };

        public Logger() {
            lock (writeLock)
            {
                logFilePath = initialize();
            }
        }

        public void setSecreets(string[] strings)
        {
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

            logFileDate = DateTime.Today;
            string logFileName = "log_" + logFileDate.ToString("MM_dd_yyyy") + ".log";
            string logFilePath = Path.Combine(logFolderPath, logFileName);

            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, getLogMessage("Initializing", 0), System.Text.Encoding.UTF8);
            }
            removeOldLogFiles(logFolderPath);

            return logFilePath;
        }

        private void removeOldLogFiles(string rootLogFolderPath)
        {
            DateTime oldestKept = DateTime.Today.AddDays(-keepLogDays);
            foreach (string file in Directory.EnumerateFiles(rootLogFolderPath, "log_*.log"))
            {
                string datePart = Path.GetFileNameWithoutExtension(file).Substring("log_".Length);
                if (DateTime.TryParseExact(datePart, "MM_dd_yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fileDate) && fileDate <= oldestKept)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException)
                    {
                        // File is in use, try again next day
                    }
                }
            }
        }

        private string getLogMessage(string text, int type = 0)
        {
            string parsedText = text;
            foreach (string secret in secreetsStrings)
            {
                parsedText = parsedText.Replace(secret, "***SECRET****");
            }

            return DateTime.Now.ToString("[MM/dd/yyyy-HH:mm.ss]") + "[" + type + "]" + parsedText + "\n";
        }

        public void writeLine(string msg, int type = 0)
        {
            Debug.WriteLine(msg);

            // Called from the UI thread, background workers and sensor tasks at the same time
            lock (writeLock)
            {
                try
                {
                    if (logFileDate != DateTime.Today)
                    {
                        logFilePath = initialize();
                    }

                    using FileStream fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    using StreamWriter streamWriter = new StreamWriter(fileStream);
                    streamWriter.Write(getLogMessage(msg, type));
                }
                catch (IOException e)
                {
                    Debug.WriteLine("Failed to write log: " + e.Message);
                }
            }
        }
    }
}
