using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PushNotificationServer.Services
{
    internal class Logger : Service {

        private const int LogFlushDelay = 100;
        private const string LogName = "Log";
        private const string LogDateTimeFormat = "HH:mm:ss MM/dd/yy ";

        private static readonly ConcurrentQueue<Tuple<int, string>> LogQueue;
        private static readonly String[] LogTypes = { "", "Warning", "Error"};
        private static readonly Dictionary<string, string> LogDirs;
        private static readonly StreamWriter[] LogWriters;

        static Logger()
        {
            LogQueue = new ConcurrentQueue<Tuple<int, string>>();
            LogDirs = new Dictionary<string, string>();
            LogWriters = new StreamWriter[LogTypes.Length];
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

        /// <summary>
        /// Returns the file for a given LogType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string GetLogFile(int type)
        {
            string key = LogTypes[type];
            if (!LogDirs.TryGetValue(key, out var fileDir))
            {
                lock (LogDirs)
                {
                    fileDir = $"{LogDir}{Path.DirectorySeparatorChar}{LogName}{key}.txt";
                    if (!File.Exists(fileDir))
                    {
                        Log($"No log file found for {key}. Creating...");
                        File.Create(fileDir).Dispose();
                    }
                }
            }
            return fileDir;
        }

        public override string Name => "Logger";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string CreateLog(string logString)
        {
            return DateTime.Now.ToString(LogDateTimeFormat) + logString;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogWithColors(TextWriter textWriter, string logString, ConsoleColor background, ConsoleColor foreground)
        {
            ConsoleColor cachebg = Console.BackgroundColor;
            ConsoleColor cachefg = Console.ForegroundColor;
            Console.BackgroundColor = background;
            Console.ForegroundColor = foreground;
            lock (textWriter) {
                textWriter.WriteLine(logString);
            }
            Console.BackgroundColor = cachebg;
            Console.ForegroundColor = cachefg;
        }

        public static void Log(string logString, int importance = -1)
        {
            if (importance != -1 && importance > VerbosityThreshhold)
                return;
            var entry = CreateLog(logString);
            Console.WriteLine(entry);
            LogQueue.Enqueue(new Tuple<int, string>(0, entry));
        }

        public static void LogWarning(string logString, int importance = -1)
        {
            if (importance != -1 && importance > VerbosityThreshhold)
                return;
            var entry = CreateLog(logString);
            LogWithColors(Console.Out, entry, ConsoleColor.Black, ConsoleColor.Yellow);
            LogQueue.Enqueue(new Tuple<int, string>(0, entry));
        }

        public static void LogError(string logString)
        {
            var entry = CreateLog(logString);
            LogWithColors(Console.Error, entry, ConsoleColor.Yellow, ConsoleColor.Red);
            LogQueue.Enqueue(new Tuple<int, string>(2, entry));
        }

        protected override void StartFunction() {
            for (var i = 0; i < LogWriters.Length; i++) {
                LogWriters[i]?.Dispose();
                LogWriters[i] = new StreamWriter(GetLogFile(i), true);
            }
        }

        protected override void StopFunction() {
            foreach (var writer in LogWriters) {
                writer?.Dispose();
            }
        }

        protected override void Job()
        {
            while (Running)
            {
                if (CrashImmediately)
                {
                    CrashImmediately = false;
                    throw new AggregateException();
                }

                while (LogQueue.TryDequeue(out var entry))
                {
                    LogWriters[entry.Item1].WriteLine(entry.Item2);
                }

                foreach (var writer in LogWriters) {
                    writer.Flush();
                }

                Thread.Sleep(LogFlushDelay);
            }
        }
    }
}