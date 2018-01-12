using System;
using System.Runtime.InteropServices;
using System.Threading;
using PushNotificationServer.Services;

namespace PushNotificationServer {
    internal abstract class Service : IComparable<Service>, IDisposable
    {
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
        ///     Any extra logic required on job start
        /// </summary>
        protected virtual void StartFunction() { }

        /// <summary>
        ///     Starts the job
        /// </summary>
        public void Start() {
            StartFunction();
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

        /// <summary>
        /// Stop the service. Blocks until Job halts.
        /// </summary>
        public void Stop() {
            StopFunction();
            Running = false;
            JobThread.Join();
            Logger.Log($"{Name} was stopped");
        }

        /// <summary>
        ///     Restart this service
        /// </summary>
        public void Restart() {
            Stop();
            Thread.Sleep(500);
            Start();
        }

        /// <summary>
        /// Stop the service
        /// </summary>
        public void Dispose() {
            Stop();
        }
    }
}