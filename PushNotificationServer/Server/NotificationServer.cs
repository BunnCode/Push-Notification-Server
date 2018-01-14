using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using PushNotificationServer.Notifications;
using PushNotificationServer.Services;

namespace PushNotificationServer.Server {
    internal class NotificationServer {
        private readonly List<Service> _services;
        private bool _running;

        /// <summary>
        ///     Server that listens for requests on the given URL
        /// </summary>
        /// <param name="boundURL">URL the server is bound to</param>
        /// <param name="writeToDisk">Should the server write to the disk?</param>
        public NotificationServer(string boundURL, int maxThreads, bool writeToDisk = false) {
            _services = new List<Service>();
            var server = new HttpServer(boundURL, maxThreads);
            if (writeToDisk) _services.Add(new Logger());
            _services.Add(server);

            var monitor = new ThreadMonitor(_services.ToArray());

            _services.Add(monitor);
            server.ProcessRequest += ProcessRequest;
        }

        /// <summary>
        ///     Latch used for disabling server
        /// </summary>
        private bool Running {
            get => _running;
            set {
                _running = value;
                if (value) return;
                Logger.Log("Server shutting down...");
                var toKill = _services.OrderByDescending(s => s).ToArray();
                foreach (var s in toKill)
                    s.Stop();
            }
        }

        /// <summary>
        ///     Restart the server
        /// </summary>
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
            foreach (var service in _services)
                service.Start();

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

        /// <summary>
        ///     Process a notification request from a client
        /// </summary>
        /// <param name="context"></param>
        private void ProcessRequest(HttpListenerContext context) {
            try {
                var request = context.Request;
                var response = context.Response;
                var requestContent = new StreamReader(request.InputStream).ReadToEnd();
                var clientInfo = JsonConvert.DeserializeObject<ClientInfo>(requestContent);
                var notificationInfo = NotificationInfoLoader.RetrieveInfo(clientInfo);
                Logger.Log(
                    $"Info requested from v{clientInfo.Version}, sending {notificationInfo.Notifications.Count} notifications.", 1);
                var messageOut = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notificationInfo));
                response.ContentLength64 = messageOut.Length;
                var output = response.OutputStream;
                output.Write(messageOut, 0, messageOut.Length);
                output.Close();
            }
            catch (Exception e) {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void CrashServer() {
            foreach (Service s in _services) {
                s.CrashImmediately = true;
                Thread.Sleep(1000);
            }
        }
    }
}