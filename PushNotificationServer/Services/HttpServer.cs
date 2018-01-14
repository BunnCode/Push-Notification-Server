using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace PushNotificationServer.Services {
    internal class HttpServer : Service {
        private readonly string _boundUrl;
        private readonly HttpListener _listener;
        private readonly Queue<HttpListenerContext> _queue;
        private readonly ManualResetEvent _stop, _ready;
        private readonly Thread[] _workers;

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

        public event Action<HttpListenerContext> ProcessRequest;

        protected override void StartFunction() {
            _stop.Reset();
            _listener.Prefixes.Add(_boundUrl);
            _listener.Start();
            Logger.Log($"Started HttpServer with {_workers.Length} threads, bound to {_boundUrl}");
            for (var i = 0; i < _workers.Length; i++) {
                _workers[i] = new Thread(Worker);
                _workers[i].Start();
            }
        }

        protected override void StopFunction() {
            _stop.Set();
            JobThread.Join();
            foreach (var worker in _workers)
                worker.Join();
            _listener.Stop();
        }

        protected override void Job() {
            while (_listener.IsListening) {
                if (CrashImmediately) {
                    CrashImmediately = false;
                    throw new AggregateException();
                }
                var context = _listener.BeginGetContext(ContextReady, null);
                if (0 == WaitHandle.WaitAny(new[] {_stop, context.AsyncWaitHandle}))
                    return;
            }
        }

        private void ContextReady(IAsyncResult ar) {
            try {
                lock (_queue) {
                    _queue.Enqueue(_listener.EndGetContext(ar));
                    _ready.Set();
                }
            }
            catch { }
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
                    Logger.Log($"Request received; {context.Request.RemoteEndPoint}", 0);
                    ProcessRequest?.Invoke(context);
                }
                catch (Exception e) {
                    Logger.Log($"Server could not process a request; {e.Message}", 0);
                }
            }
        }
    }
}