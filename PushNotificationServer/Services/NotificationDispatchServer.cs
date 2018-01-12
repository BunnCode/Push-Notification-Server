using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using PushNotificationServer.Notifications;

namespace PushNotificationServer.Services {
    internal class NotificationDispatchServer : Service {
        private readonly string _boundUrl;

        /// <summary>
        ///     Listener used for the dispatch
        /// </summary>
        private HttpListener _dispatchListener;

        /// <summary>
        ///     Server that handles dispatching jobs to serve Notification requests
        /// </summary>
        /// <param name="boundUrl">URL the server is bound to</param>
        public NotificationDispatchServer(string boundUrl) {
            _boundUrl = boundUrl;
        }

        public override string Name => "Notification Dispatch Server";

        protected override void Job() {
            _dispatchListener = new HttpListener();
            try {
                {
                    _dispatchListener.Prefixes.Add(_boundUrl);
                    _dispatchListener.Start();
                    while (Running)
                        try {
                            var context = _dispatchListener.GetContext();
                            ThreadPool.QueueUserWorkItem(delegate {
                                var request = context.Request;
                                var response = context.Response;
                                var requestContent = new StreamReader(request.InputStream).ReadToEnd();
                                var clientInfo = JsonConvert.DeserializeObject<ClientInfo>(requestContent);
                                Logger.Log($"Info requested from v{clientInfo.Version}");
                                var notificationInfo = NotificationInfoLoader.RetrieveInfo(clientInfo);
                                var messageOut = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notificationInfo));
                                response.ContentLength64 = messageOut.Length;
                                var output = response.OutputStream;
                                output.Write(messageOut, 0, messageOut.Length);
                                output.Close();
                            });
                        }
                        catch (Exception e) {
                            Logger.Log($"Dispatch threw \"{e.Message}\", was caught successfully");
                        }

                    try {
                        _dispatchListener?.Stop();
                    }
                    catch (ObjectDisposedException) { }
                }
            }
            catch (Exception e) {
                Logger.Log($"Crash!!! Dispatcher threw \"{e.Message}\", which was unhandled!");
            }

            Logger.Log("Work Dispatch server terminated.");
        }

        protected override void StopFunction() {
            _dispatchListener.Abort();
        }
    }
}