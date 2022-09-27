﻿using System;
using System.Diagnostics;
using System.IO;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace HA_Desktop_Companion.Libraries {
    public class Logging {
        public string logFile;
        private StreamWriter sw;
        private string lastMessage = "";
        private int lastMessageCount = 0;


        public Logging(string logFilePath) {
            FileStream fs = File.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
            logFile = logFilePath;
        }

        public void Write(string msg, bool logOnly = false)
        {
            if (sw.BaseStream == null)
            {
                Debug.WriteLine("SW/CLOSED");
                return;
            }

            if (lastMessage == msg)
            {
                lastMessageCount++;
            } else
            {
                string outMsg = lastMessage;
                if (lastMessageCount > 0)
                {
                    outMsg = " (" + lastMessageCount + "x)";
                    if (!logOnly)
                        Debug.WriteLine(outMsg);

                    sw.WriteLine("[" + DateTime.Now.ToString("yyyyMMddTHHmmss") + "]-" + outMsg);
                } else
                {
                    if (!logOnly)
                        Debug.WriteLine("");
                    sw.WriteLine("");
                }

                lastMessageCount = 0;
                if (!logOnly)
                    Debug.Write(msg);

                sw.Write("[" + DateTime.Now.ToString("yyyyMMddTHHmmss") + "]-" + msg);
            }

            lastMessage = msg;
        }

        public void housekeeping()
        {
            if (sw.BaseStream.Length > (1024 * 30)) //30MB
            {
                sw.Close();
                File.Delete(logFile);
                FileStream fs = File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.Read);
                sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
            }

        }
    }
}
