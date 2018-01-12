using System;
using System.Threading;
using PushNotificationServer.Services;

namespace PushNotificationServer {
    internal abstract class Service : IComparable<Service> {
        /// <summary>
        ///     The thread that this job is running on
        /// </summary>
        public Thread JobThread;

        /// <summary>
        ///     Priority. Lower-priority services get started last and halted first.
        /// </summary>
        protected virtual int Priority { get; }

        protected virtual bool Running { get; set; }

        public abstract string Name { get; }

        public int CompareTo(Service other) {
            return Priority - other.Priority;
        }

        /// <summary>
        ///     The Job that this service does
        /// </summary>
        protected abstract void Job();

        /// <summary>
        ///     Starts the job
        /// </summary>
        public void Start() {
            Logger.Log($"{Name} started!");
            Running = true;
            if (JobThread != null)
                while (JobThread.IsAlive)
                    Thread.Sleep(100);

            JobThread = new Thread(Job);
            JobThread.Start();
        }

        /// <summary>
        ///     Any extra logic required for halting the Job
        /// </summary>
        protected virtual void StopFunction() { }

        public void Stop() {
            Running = false;
            StopFunction();
            Logger.Log($"{Name} was stopped");
        }

        /// <summary>
        ///     Restart this service
        /// </summary>
        public void Restart() {
            Stop();
            Thread.Sleep(100);
            Start();
        }
    }
}