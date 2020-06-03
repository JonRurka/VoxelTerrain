using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace UnityGameServer
{
    public class Logger
    {
        public enum LogLevel
        {
            Print,
            Log,
            Warning,
            Error,
        }

        public struct LogEntry
        {
            public string Message;
            public LogLevel Level;

            public LogEntry(LogLevel level, string message)
            {
                Level = level;
                Message = message;
            }
        }

        private static char sepChar { get { return Path.DirectorySeparatorChar; } }
        private static string _inputStr = string.Empty;
        private static string _logFile = string.Empty;
        private static int _messageCount = 0;
        public const int MAX_MESSAGES = 8000;
        public static string LogFile { get { return _logFile; } }
        public static string InputStr
        {
            get
            {
                return _inputStr;
            }
            set
            {
                _inputStr = value;
                AddEntry(LogLevel.Print, "");
            }
        }
        public static event Action<LogLevel, string> NonConsoleLog;

        private static List<LogEntry> _entries = new List<LogEntry>();
        private static List<LogEntry> _currentEntries = new List<LogEntry>();

        public static void Print(object message, params object[] args)
        {
            SafeDebug.Log("[Server]: "+ string.Format(message.ToString(), args));
            /*AddEntry(LogLevel.Print, string.Format(message.ToString(), args));
            Action a = () => LogToFile(string.Format(message.ToString(), args));
            QueueLog(a);*/
        }

        public static void PrintNoFormat(object message)
        {
            SafeDebug.Log("[Server]: " + message);
            /*AddEntry(LogLevel.Print, message.ToString());
            Action a = () => LogToFile(message.ToString());
            QueueLog(a);*/
        }

        public static void Log(object message, params object[] args)
        {
            SafeDebug.Log("[Server]: " + string.Format(message.ToString(), args));
            /*string messageStr = string.Format(message.ToString(), args);
            AddEntry(LogLevel.Log, string.Format("[{0}]: {1}", GetTime(), messageStr));
            Action a = () => LogToFile(string.Format("[{0}]: {1}", GetTime(), messageStr));
            QueueLog(a);*/
        }

        public static void LogWarning(object message, params object[] args)
        {
            SafeDebug.LogWarning("[Server]: " + string.Format(message.ToString(), args));
            /*string messageStr = string.Format(message.ToString(), args);
            AddEntry(LogLevel.Warning, string.Format("[{0}]: {1}", GetTime(), messageStr));
            Action a = () => LogToFile(string.Format("[{0} W]: {1}", GetTime(), messageStr));
            QueueLog(a);*/
        }

        public static void LogError(object message, params object[] args)
        {
            SafeDebug.LogError("[Server]: " + string.Format(message.ToString(), args));
            /*string messageStr = string.Format(message.ToString(), args);
            AddEntry(LogLevel.Error, string.Format("[{0}]: {1}", GetTime(), messageStr));
            Action a = () => LogToFile(string.Format("[{0} E]: {1}", GetTime(), messageStr));
            QueueLog(a);*/
        }

        public static void Clear()
        {
            //InputStr = string.Empty;
            //Console.Clear();
            //AddEntry(LogLevel.Print, "");
        }

        public static string GetTime()
        {
            DateTime time = DateTime.Now;
            return string.Format("{0:00}:{1:00}:{2:00}", time.Hour, time.Minute, time.Second);
        }

        public static string GetDateTime()
        {
            DateTime time = DateTime.Now;
            return time.ToString();
        }

        public static long GetEpoch()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        private static void QueueLog(Action a)
        {
            //TaskQueue.QueueMain(a);
        }

        public static void LogToFile(string message)
        {
            /*if (!Directory.Exists(ServerBase.BaseInstance.AppDirectory + "SystemLogs" + sepChar))
                Directory.CreateDirectory(ServerBase.BaseInstance.AppDirectory + "SystemLogs" + sepChar);

            if (message == string.Empty)
                return;

            if (_logFile == string.Empty)
            {
                _logFile = GetNewFileName();
            }
            //Log(_logFile);
            StreamWriter writer = new StreamWriter(_logFile, true);
            writer.WriteLine(message.ToString());
            writer.Close();*/
        }

        public static string GetNewFileName()
        {
            return "";
            /*if (!Directory.Exists(ServerBase.BaseInstance.AppDirectory + "SystemLogs" + sepChar))
                Directory.CreateDirectory(ServerBase.BaseInstance.AppDirectory + "SystemLogs" + sepChar);
            int fileCount = Directory.GetFiles(ServerBase.BaseInstance.AppDirectory + "SystemLogs" + sepChar).Length;
            return ServerBase.BaseInstance.AppDirectory + "SystemLogs" + sepChar + fileCount + "_" +
                   GetDateTime().Replace("/", "-").Replace(" ", "_").Replace(":", ";") + ".txt";*/
        }

        public static void Update()
        {
            if (_entries.Count > 0)
            {
                lock (_entries)
                {
                    _currentEntries.Clear();
                    _currentEntries.AddRange(_entries);
                    _entries.Clear();
                }

                for (int i = 0; i < _currentEntries.Count; i++)
                {
                    Draw(_currentEntries[i].Level, _currentEntries[i].Message);
                }
            }
        }

        private static void AddEntry(LogLevel level, string message)
        {
            _entries.Add(new LogEntry(level, message));
        }

        private static void Draw(LogLevel level, string message = "")
        {
            /*if (!ARServer.Instance.IsConsole) {
                NonConsoleLog(level, message);
                return;
            }*/

            ClearCurrentConsoleLine();
            if (message != "")
            {
                _messageCount++;
                if (_messageCount >= MAX_MESSAGES)
                {
                    Console.Clear();
                    _messageCount = 0;
                }
                ConsoleColor color = Console.ForegroundColor;
                switch (level)
                {
                    case LogLevel.Print:
                    case LogLevel.Log:
                        break;
                    case LogLevel.Warning:
                        color = ConsoleColor.Yellow;
                        break;
                    case LogLevel.Error:
                        color = ConsoleColor.Red;
                        break;
                }
                Console.ForegroundColor = color;
                Console.WriteLine(message);
            }
            Console.ResetColor();
            Console.Write("> {0}", InputStr);
        }

        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursorTop = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursorTop);
        }
    }
}