using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PushNotificationServer.Services
{
    internal class Logger : Service
    {
        private const string LogName = "Log.txt";
        private const string ErrorLogName = "CriticalLog.txt";
        private const string LogDateTimeFormat = "HH:mm:ss MM/dd/yy ";
        private static readonly ConcurrentQueue<string> LogQueue;
        private static readonly ConcurrentQueue<string> ErrorLogQueue;
        static Logger()
        {
            LogQueue = new ConcurrentQueue<string>();
            ErrorLogQueue = new ConcurrentQueue<string>();
            VerbosityThreshhold = 1;
        }

        public static int VerbosityThreshhold { get; set; }


        private static string _logDir;
        /// <summary>
        ///     The directory in which logs are saved
        /// </summary>
        private static string LogDir {
            get {
                if (_logDir == null)
                {
                    _logDir = AppDomain.CurrentDomain.BaseDirectory;
                    if (!Directory.Exists(_logDir))
                        Directory.CreateDirectory(_logDir);
                }

                return _logDir;
            }
        }


        private static string _logFile;
        /// <summary>
        ///     The file in which logs are saved
        /// </summary>
        private static string LogFile {
            get {
                if (_logFile == null)
                {
                    _logFile = LogDir + Path.DirectorySeparatorChar + LogName;
                    if (!File.Exists(_logFile))
                    {
                        Log("No log file found. Creating...");
                        File.Create(_logFile);
                    }
                }
                return _logFile;
            }
        }

        private static string _errorLogFile;
        /// <summary>
        ///     The file in which error logs are saved
        /// </summary>
        private static string ErrorLogFile {
            get {
                if (_errorLogFile == null)
                {
                    _errorLogFile = LogDir + Path.DirectorySeparatorChar + ErrorLogName;
                    if (!File.Exists(LogFile))
                    {
                        Log("No critical log file found. Creating...");
                        File.Create(_errorLogFile);
                    }
                }

                return _errorLogFile;
            }
        }

        public override string Name => "Logger";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string CreateLog(string logString)
        {
            return DateTime.Now.ToString(LogDateTimeFormat) + logString;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogWithColors(string logString, ConsoleColor background, ConsoleColor foreground)
        {
            ConsoleColor cachebg = Console.BackgroundColor;
            ConsoleColor cachefg = Console.ForegroundColor;
            Console.BackgroundColor = background;
            Console.ForegroundColor = foreground;
            Console.WriteLine(logString);
            Console.BackgroundColor = cachebg;
            Console.ForegroundColor = cachefg;
        }

        public static void Log(string logString, int importance = -1)
        {
            if (importance != -1 && importance > VerbosityThreshhold)
                return;
            var entry = CreateLog(logString);
            Console.WriteLine(entry);
            LogQueue.Enqueue(entry);
        }

        public static void LogError(string logString)
        {
            var entry = CreateLog(logString);
            LogWithColors(logString, ConsoleColor.Yellow, ConsoleColor.Red);
            ErrorLogQueue.Enqueue(entry);
        }

        protected override void Job()
        {
            new Thread(() => WriteLogsToDisk(LogFile, LogQueue)).Start();
            new Thread(() => WriteLogsToDisk(ErrorLogFile, ErrorLogQueue)).Start();
        }

        private void WriteLogsToDisk(String file, ConcurrentQueue<string> queue) {
            using (var outputFile = new StreamWriter(file, true))
            {
                outputFile.AutoFlush = true;
                while (Running)
                {
                    if (CrashImmediately)
                    {
                        CrashImmediately = false;
                        throw new AggregateException();
                    }

                    while (queue.TryDequeue(out var entry))
                    {
                        outputFile.WriteLine(entry);
                    }
                    Thread.Sleep(100);
                }
                outputFile.Dispose();
            }
        }
    }
}