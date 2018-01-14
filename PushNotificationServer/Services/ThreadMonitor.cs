using System;
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
            foreach (Service s in monitoringServices) {
                AddService(s);
            }

            //yeah if this works this class's utility is essentially 0
            Crash += RestartService;
        }

        /// <inheritdoc />
        public override string Name => "Thread Monitor";

        /// <inheritdoc />
        protected override int Priority => 1;

        /// <summary>
        ///     Add a service to be monitored
        /// </summary>
        /// <param name="service">The service to monitor</param>
        public void AddService(Service service) {
            _monitoringServices.Add(service);
            service.Crash += HandleCrash;
        }

        private void HandleCrash(Service service) {
            if (service.Running) {
                RestartService(service);
            }
        }

        private void RestartService(Service service) {
            Logger.LogWarning($"Service {service.Name} crashed! Attempting to restart..");
            try {
                service.Restart();
            }
            catch (Exception e) {
                Logger.LogError($"Service {service.Name} could not be restarted: {e.Message} : {e.StackTrace}");
            }
            Logger.LogWarning($"Service {service.Name} restarted.");
        }

        /// <summary>
        ///     Stop monitoring a service
        /// </summary>
        /// <param name="service">The service to remove</param>
        public void RemoveService(Service service) {
            _monitoringServices.Add(service);
        }

        /// <inheritdoc />
        protected override void Job() {
            while (Running) {
                //Seems like ThreadMonitor might be outdated now the events are in play
                Thread.Sleep(CheckInterval);
            }
        }
    }
}