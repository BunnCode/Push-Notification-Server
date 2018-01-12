using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace PushNotificationServer.Services {
    internal class Logger : Service {
        private const string LogName = "Log.txt";
        private static string _logDir;
        private static readonly ConcurrentQueue<string> _logQueue;

        static Logger() {
            _logQueue = new ConcurrentQueue<string>();
            VerbosityLevel = 1;
        }

        public static int VerbosityLevel { get; set; }

        /// <summary>
        ///     The directory in which logs are saved
        /// </summary>
        private static string LogFile {
            get {
                if (_logDir == null) {
                    _logDir = Directory.GetCurrentDirectory();
                    if (!Directory.Exists(_logDir))
                        Directory.CreateDirectory(_logDir);
                }

                return _logDir + Path.DirectorySeparatorChar + LogName;
            }
        }

        public override string Name => "Logger";

        public static void Log(string logString, int verbosityLevel = -1) {
            if (verbosityLevel != -1 && verbosityLevel < VerbosityLevel)
                return;
            var entry = DateTime.Now.ToString("HH:mm:ss MM/dd/yy ") + logString;
            Console.WriteLine(entry);
            _logQueue.Enqueue(entry);
        }

        protected override void Job() {
            try {
                Log("Logging thread started!");
                if (!File.Exists(LogFile)) {
                    Log("No log file found. Creating...");
                    File.Create(LogFile);
                }

                using (var outputFile = new StreamWriter(LogFile, true)) {
                    while (Running)
                        try {
                            while (_logQueue.TryDequeue(out var entry)) outputFile.WriteLine(entry);

                            outputFile.WriteLine();
                            Thread.Sleep(100);
                        }
                        catch (Exception e) {
                            Log($"Logger threw \"{e.Message}\", was caught successfully");
                        }
                }
            }
            catch (Exception e) {
                Log($"Crash!!! Logger threw \"{e.Message}\", which was unhandled!");
            }

            Log("Logging thread terminated.");
        }
    }
}