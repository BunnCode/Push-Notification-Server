using System;
using System.Collections.Generic;
using System.Threading;
using Mono.Options;
using PushNotificationServer.Notifications;

namespace PushNotificationServer {
    internal class Program {
        private static bool _running = true;

        private static int Main(string[] args) {
            var url = "http://+:80/";
            var help = false;
            var writeToDisk = true;
            var p = new OptionSet {
                {"u|url", $"The URL to bind to, including port. (Default: {url})", v => url = v}, {
                    "w", $"Flag to indicate that logs should not be written to disk (Default: {writeToDisk})",
                    v => writeToDisk = v == null
                },
                {"h|?|help", "Show this dialog", v => help = v != null}
            };

            var extra = p.Parse(args);
            if(extra.Count > 0)
                Console.WriteLine($"Unknown commands: {string.Join(", ", extra)}");

            if (help) {
                p.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            var server = new NotificationServer(url, writeToDisk);

            #region commands

            var commands = new Dictionary<string, Action>();
            commands.Add("list",
                () => Console.WriteLine($"Active Notifications: " +
                                        $"{Environment.NewLine}{NotificationInfoLoader.ToString()}"));
            commands.Add("restart", server.Restart);
            commands.Add("reload", NotificationInfoLoader.Reload);
            commands.Add("exit", () => { _running = false; });
            commands.Add("help", () => Console.WriteLine($"Commands: {string.Join(", ", commands.Keys)}"));

            #endregion

            server.Start();
            Thread.Sleep(100);
            //touchey touchey (Forces static ctor to trigger)
            NotificationInfoLoader.ToString();
            Console.WriteLine(Environment.NewLine + "Type 'help' for a list of commands");
            while (_running) {
                Console.Write('>');
                var command = Console.ReadLine()?.ToLower();
                if (command == null) continue;
                Console.WriteLine();
                if (!commands.TryGetValue(command, out var action))
                    action = () => Console.WriteLine("Invalid command. Type 'help' for a list of commands.");
                action();
                Thread.Sleep(300);
            }

            server.Stop();
            Console.ReadLine();
            return 0;
        }
    }
}