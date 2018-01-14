using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using PushNotificationServer.Services;

namespace PushNotificationServer.Notifications {
    /// <summary>
    ///     Loads Notification Info from Disk. Files are loaded with the following schema:
    ///     ./{NotificationDirName}/{Product}/Notification Files. Notifications in the base
    ///     ./{NotificationDirName}/ directory will be served to ALL clients, regardless of
    ///     product.
    /// </summary>
    public static class NotificationInfoLoader {

        private const String NotificationDirName = "Notifications";

        private static string _notificationDir;
        private static ConcurrentDictionary<string, List<Tuple<int[], NotificationInfo.Notification>>> _notifications;
        private static ConcurrentDictionary<string, NotificationInfo> _notificationDict;

        static NotificationInfoLoader() {
            Reload();
        }

        /// <summary>
        ///     The directory in which notifications are saved
        /// </summary>
        private static string NotificationDir {
            get {
                if (_notificationDir == null) {
                    _notificationDir = AppDomain.CurrentDomain.BaseDirectory +
                                       Path.DirectorySeparatorChar +
                                       NotificationDirName + Path.DirectorySeparatorChar;
                    if (!Directory.Exists(_notificationDir))
                        Directory.CreateDirectory(_notificationDir);
                }
                return _notificationDir;
            }
        }

        /// <summary>
        ///     Reload the notification files. Call this when notifications are changed.
        /// </summary>
        public static void Reload() {
            _notifications = new ConcurrentDictionary<string, List<Tuple<int[], NotificationInfo.Notification>>>();
            _notificationDict = new ConcurrentDictionary<string, NotificationInfo>();
            LoadAllFiles();
        }

        /// <summary>
        ///     Add a new notification to the server.
        /// </summary>
        /// <param name="product">product to add</param>
        /// <param name="version">Version above which to not show the notification to</param>
        /// <param name="n">Notification to add</param>
        public static void AddNotification(string product, int[] version, NotificationInfo.Notification n) {
            if (!_notifications.TryGetValue(product, out var notificationList)) {
                notificationList = new List<Tuple<int[], NotificationInfo.Notification>>();
                _notifications.TryAdd(product, notificationList);
            }
           notificationList.Add(new Tuple<int[], NotificationInfo.Notification>(version, n));
        }

        /// <summary>
        ///     Write all notifications active in memory to the disk
        /// </summary>
        public static void WriteAllFiles() {
            foreach (var k in _notifications.Keys) {
                foreach(var v in _notifications[k])
                    WriteFile(v.Item1, v.Item2);
            }
        }

        /// <summary>
        ///     Retrieve all notifications for a given version
        /// </summary>
        /// <param name="client">The client version to check</param>
        /// <returns>Notifications relevant to this version</returns>
        public static NotificationInfo RetrieveInfo(ClientInfo client) {
            if (_notificationDict.TryGetValue(client.Version, out var info))
                return info;

            info = new NotificationInfo();
            var version = client.GetVersionInfo();
            
            if (_notifications.TryGetValue(client.Product, out var notificationList)) {
                foreach (var n in notificationList)
                    if (n.Item1[0] >= version[0])
                        if (n.Item1[1] >= version[1])
                            if (n.Item1[2] >= version[2])
                                info.AddNotification(n.Item2);
            }

            _notificationDict.TryAdd(client.Version, info);
            return info;
        }

        /// <summary>
        ///     Returns a string representation of the current notifications
        /// </summary>
        /// <returns></returns>
        public new static string ToString() {
            var builder = new StringBuilder();
            var i = 0;
            foreach (var notification in _notifications.Keys) {
                if (_notifications.TryGetValue(notification, out var notificationList))
                {
                    foreach (var n in notificationList)
                        builder.Append($"Notification {i++}, for version: ({string.Join(", ", n.Item1)}) and lower of product {notification}:" +
                                       $"{Environment.NewLine}\"\"\"" +
                                       $"{Environment.NewLine}{n.Item2.Message}" +
                                       $"{Environment.NewLine}\"\"\"" +
                                       $"{Environment.NewLine}");
                }
            }
            return builder.ToString();
        }


        private static void LoadAllFiles() {
            LoadFoldersAction(null, NotificationDir);

            void LoadFoldersAction(string productDir, string dir) {
                foreach (var d in Directory.GetDirectories(dir)) {
                    if (productDir == null || productDir.Equals(NotificationDirName)) {
                        productDir = Path.GetFileName(d);
                    }     
                    LoadFoldersAction(productDir, d);
                }    
                LoadFileAction(productDir, dir);
            }

            void LoadFileAction(string productDir, string dir) {
                foreach (var f in Directory.GetFiles(dir)) {
                    LoadFile(productDir, f);
                    if (productDir == null)
                        Logger.LogError($"Warning! Could not get product for message {Path.GetFileName(f)}," +
                                        $" message will be shown to all users regardless of product!");
                }          
            }

            Console.WriteLine(
                $"Loaded {_notifications.Count} notifications from dir " +
                $"{GetRelativePath(NotificationDir, AppDomain.CurrentDomain.BaseDirectory)}. " +
                $"Type 'list' to view.");
        }

        private static void LoadFile(string productName, string fileName)
        {
            using (var inputFile = new StreamReader(fileName))
            {
                var input = inputFile.ReadToEnd();
                var n = JsonConvert.DeserializeObject<Tuple<int[], NotificationInfo.Notification>>(input);
                AddNotification(productName, n.Item1, n.Item2);
            }
        }

        private static string GetNotificationDir(string id) {
            return NotificationDir + id + ".txt";
        }

        private static void WriteFile(int[] version, NotificationInfo.Notification notification) {
            using (var outputFile = new StreamWriter(GetNotificationDir(notification.Id.ToString()))) {
                outputFile.WriteLine(
                    JsonConvert.SerializeObject(
                        new Tuple<int[], NotificationInfo.Notification>(version, notification)));
            }
        }

      

        private static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }
    }
}