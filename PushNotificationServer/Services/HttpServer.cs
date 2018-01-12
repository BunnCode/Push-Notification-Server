using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using PushNotificationServer.Notifications;

namespace PushNotificationServer.Services {
    internal class HttpServer : Service {
        public event Action<HttpListenerContext> ProcessRequest;
        private readonly string _boundUrl;
        private readonly ManualResetEvent _stop, _ready;
        private readonly Thread[] _workers;
        private readonly Queue<HttpListenerContext> _queue;
        private readonly HttpListener _listener;

        /// <summary>
        ///     Server that handles dispatching jobs to serve Notification requests
        /// </summary>
        /// <param name="boundUrl">URL the server is bound to</param>
        /// <param name="maxThreads">Maximum number of threads the server can occupy</param>
        public HttpServer(string boundUrl, int maxThreads) {
            _boundUrl = boundUrl;
            _stop = new ManualResetEvent(false);
            _ready = new ManualResetEvent(false);
            _queue = new Queue<HttpListenerContext>();
            _listener = new HttpListener();
            _workers = new Thread[maxThreads];
        }


        public override string Name => "Notification Dispatch Server";

        protected override void StartFunction() {
            _stop.Reset();
            Logger.Log($"Started HttpServer with {_workers.Length} threads.");
            _listener.Prefixes.Add(_boundUrl);
            _listener.Start();

            for (int i = 0; i < _workers.Length; i++) {
                _workers[i] = new Thread(Worker);
                
            }
            for (int i = 0; i < _workers.Length; i++)
            {
                _workers[i].Start();
            } 
        }

        protected override void StopFunction()
        {
            _stop.Set();
            JobThread.Join();
            foreach (Thread worker in _workers)
                worker.Join();
            _listener.Stop();
        }

        protected override void Job()
        {
            try
            {
                while (_listener.IsListening) {
                    try
                    {
                        var context = _listener.BeginGetContext(ContextReady, null);
                        if (0 == WaitHandle.WaitAny(new[] {_stop, context.AsyncWaitHandle}))
                            goto EXIT;
                    }
                    catch (Exception e)
                    {
                        Logger.Log($"Dispatch threw \"{e.Message}\", was caught successfully");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Crash!!! Dispatcher threw \"{e.Message}\", which was unhandled!");
            }
            EXIT:
            Logger.Log("Work Dispatch server terminated.");
        }

        private void ContextReady(IAsyncResult ar)
        {
            try
            {
                lock (_queue)
                {
                    _queue.Enqueue(_listener.EndGetContext(ar));
                    _ready.Set();
                }
            }
            catch { return; }
        }

        private void Worker() {
            WaitHandle[] wait = {_ready, _stop};
            while (0 == WaitHandle.WaitAny(wait)) {
                HttpListenerContext context;
                lock (_queue) {
                    if (_queue.Count > 0) {
                        context = _queue.Dequeue();
                    }
                    else {
                        _ready.Reset();
                        continue;
                    }
                }

                try {
                    Logger.Log($"Request recieved; {context.Request.UserHostAddress}");
                    ProcessRequest?.Invoke(context);
                }
                catch (Exception e) {
                    Logger.Log($"Server could not process a request; {e.Message}");
                }
            }
        }
    }
}