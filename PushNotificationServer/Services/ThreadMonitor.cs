using System.Collections.Generic;
using System.Threading;

namespace PushNotificationServer.Services {
    internal class ThreadMonitor : Service {
        private const int CheckInterval = 100;
        private readonly List<Service> _monitoringServices = new List<Service>();

        /// <summary>
        ///     Service that monitors Services to ensure that they s
        /// </summary>
        /// <param name="monitoringServices"></param>
        public ThreadMonitor(params Service[] monitoringServices) {
            _monitoringServices.AddRange(monitoringServices);
        }

        public override string Name => "Thread Monitor";

        /// <summary>
        ///     Add a service to be monitored
        /// </summary>
        /// <param name="service">The service to monitor</param>
        public void AddService(Service service) {
            _monitoringServices.Add(service);
        }

        /// <summary>
        ///     Stop monitoring a service
        /// </summary>
        /// <param name="service">The service to remove</param>
        public void RemoveService(Service service) {
            _monitoringServices.Add(service);
        }

        protected override void Job() {
            while (Running) {
                foreach (var s in _monitoringServices)
                    if (!s.JobThread.IsAlive)
                        Logger.Log($"{s.Name} crashed! Restarting...");
                Thread.Sleep(CheckInterval);
            }
        }
    }
}