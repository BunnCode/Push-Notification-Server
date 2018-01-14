using System;
using System.Collections.Generic;

namespace PushNotificationServer.UserIOData {
    [Serializable]
    public class ClientInfo {
        public string Version;
        public string Product;

        /// <summary>
        ///     Get the version info as an array
        /// </summary>
        /// <returns>Version Info array</returns>
        public int[] GetVersionInfo() {
            return Array.ConvertAll(Version.Split('.'), int.Parse);
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            var version = GetVersionInfo();
            var hash = version[0] * 17;
            hash *= version[1] * 17;
            hash *= version[2] * 17;
            return hash;
        }
    }
}