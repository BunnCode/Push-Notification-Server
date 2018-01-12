using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PushNotificationServer.Services;

namespace PushNotificationServer {
    internal class NotificationServer {
        private readonly List<Service> _services;
        private bool _running;


        /// <summary>
        ///     Server that listens for requests on the given URL
        /// </summary>
        /// <param name="boundURL">URL the server is bound to</param>
        /// <param name="writeToDisk">Should the server write to the disk?</param>
        public NotificationServer(string boundURL, bool writeToDisk = false) {
            Service logger = new Logger();
            Service server = new NotificationDispatchServer(boundURL);
            Service monitor = new ThreadMonitor(logger, server);
            _services = new List<Service> {logger, server, monitor};
        }

        /// <summary>
        ///     Latch used for disabling server
        /// </summary>
        public bool Running {
            get => _running;
            set {
                _running = value;
                if (!value) {
                    var toKill = _services.OrderByDescending(s => s).ToArray();
                    foreach (var s in toKill)
                        s.Stop();
                }
            }
        }

        public void Restart() {
            Stop();
            Logger.Log("Restarting Server...");
            Logger.Log("Shutting Server Down...");
            Thread.Sleep(1000);
            Logger.Log("Rebooting Server...");
            Start();
        }


        /// <summary>
        ///     Start the server
        /// </summary>
        public void Start() {
            Logger.Log("Server Started.");
            Running = true;

            _services.Sort();

            //Start up the services
            foreach (var service in _services) service.Start();

            foreach (var service in _services)
                if (!service.JobThread.IsAlive)
                    Thread.Sleep(100);

            Logger.Log("Server started successfully.");
        }

        /// <summary>
        ///     Stop the server
        /// </summary>
        public void Stop() {
            Running = false;
        }
    }
}