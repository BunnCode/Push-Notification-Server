using System;
using System.Threading;
using PushNotificationServer.Services;

namespace PushNotificationServer
{
    internal abstract class Service : IComparable<Service>, IDisposable
    {
        /// <summary>
        ///     The thread that this job is running on
        /// </summary>
        public Thread JobThread;

        /// <summary>
        ///     Priority. Lower-priority services get started last and halted first.
        /// </summary>
        protected virtual int Priority => 0;

        protected virtual bool Running { get; set; }

        /// <summary>
        ///     The name of this service
        /// </summary>
        public abstract string Name { get; }

        public int CompareTo(Service other)
        {
            return Priority - other.Priority;
        }

        /// <summary>
        ///     Stop the service
        /// </summary>
        public void Dispose()
        {
            Stop();
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
        public void Start()
        {
            StartFunction();
            Logger.Log($"{Name} started!");
            Running = true;
            if (JobThread != null)
                while (JobThread.IsAlive)
                    Thread.Sleep(100);

            JobThread = new Thread(() => {
                try
                {
                    Job();
                }
                catch (Exception e)
                {
                    Logger.LogError(
                        $"Crash!!! {Name} crashed with the following exception:{e.Message}, StackTrace: {e.StackTrace}");
                    Crash?.Invoke(this);
                }
            });
            JobThread.Start();
        }

        /// <summary>
        ///     Any extra logic required for halting the Job
        /// </summary>
        protected virtual void StopFunction() { }

        /// <summary>
        ///     Stop the service. Blocks until Job halts.
        /// </summary>
        public void Stop()
        {
            StopFunction();
            Running = false;
            JobThread.Join();
            Logger.Log($"{Name} was stopped");
        }

        /// <summary>
        ///     Restart this service
        /// </summary>
        public void Restart()
        {
            Stop();
            Start();
        }

        /// <summary>
        ///     Set to true to immediately crash the service- used for testing.
        /// </summary>
        public bool CrashImmediately { get; set; }

        /// <summary>
        ///     Event raised when the Service crashes.
        /// </summary>
        public event Action<Service> Crash;
    }
}