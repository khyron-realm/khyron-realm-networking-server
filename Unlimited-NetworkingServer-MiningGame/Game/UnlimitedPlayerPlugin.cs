using System;
using System.ComponentModel;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    /// <summary>
    ///     Player manager that handles the game messages
    /// </summary>
    public class UnlimitedPlayerPlugin : Plugin
    {
        private static readonly object InitializeLock = new object();

        private UnlimitedLoginPlugin _loginPlugin;

        public UnlimitedPlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        /// <summary>
        ///     Player connected handler that initializes the database and sends connection confirmation to client
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            if (_loginPlugin == null)
                lock (InitializeLock)
                {
                    if (_loginPlugin == null)
                        _loginPlugin = PluginManager.GetPluginByType<UnlimitedLoginPlugin>();
                }

            using (var msg = Message.CreateEmpty(GameTags.PlayerConnected))
            {
                e.Client.SendMessage(msg, SendMode.Reliable);
            }

            e.Client.MessageReceived += OnMessageReceived;
        }

        /// <summary>
        ///     Player disconnected handler that removes the client
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e) {}

        /// <summary>
        ///     Message received handler that receives each message and executes the necessary actions
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Game + 1))
                    return;
                
                var client = e.Client;
                
                /* TO BE ACTIVATED to check the authentication
                 
                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, GameTags.RequestFailed, "Player not logged in.")) return;
                */
                    
                switch (message.Tag)
                {
                    case GameTags.PlayerData:
                    {
                        SendPlayerData(client);
                        break;
                    }

                    case GameTags.ConversionStatus:
                    {
                        bool conversionStatus = true;
                        DateTime remainingTime = DateTime.Now;
                        
                        // Check if there are any conversion in progress
                        if (conversionStatus == true)
                        {
                            using (var newPlayerWriter = DarkRiftWriter.Create())
                            {
                                newPlayerWriter.Write(remainingTime.ToBinary());

                                using (var newPlayerMessage = Message.Create(GameTags.PlayerData, newPlayerWriter))
                                {
                                    client.SendMessage(newPlayerMessage, SendMode.Reliable);
                                }
                            }
                        }
                        else
                        {
                            // Send conversion not available
                            using (var msg = Message.CreateEmpty(GameTags.ConversionNotAvailable))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }


                        break;
                    }

                    case GameTags.ConvertResources:
                    {
                        bool conversionResult = true;
                        Logger.Info("Converting resources");
                        
                        // Check if resources are available
                        if (conversionResult == true)
                        {
                            // Yes: Add resources to conversion
                            
                            // Send conversion accepted
                            using (var msg = Message.CreateEmpty(GameTags.ConversionAccepted))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                        else
                        {
                            // Send conversion rejected
                            using (var msg = Message.CreateEmpty(GameTags.ConversionRejected))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Sends player data to the client
        /// </summary>
        /// <param name="client">The connected client</param>
        private void SendPlayerData(IClient client)
        {
            // Retrieve data from database
            string id = "abc";
            byte level = 10;
            ushort experience = 2;
            uint energy = 7;

            var silicon = new Resource(0, "silicon", 10, 100);
            var lithium = new Resource(1, "lithium", 5, 50);
            var titanium = new Resource(2, "titanium", 20, 300);

            var worker = new Robot(0, "worker", 1, 1, 1, 1);
            var probe = new Robot(1, "probe", 2, 2, 2, 2);
            var crusher = new Robot(2, "crusher", 3, 3, 3, 3);

            var newPlayerData = new PlayerData(id, level, experience, energy, silicon, lithium, titanium, worker, probe, crusher);

            // Send data to the client
            using (var newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayerData);

                using (var newPlayerMessage = Message.Create(GameTags.PlayerData, newPlayerWriter))
                {
                    client.SendMessage(newPlayerMessage, SendMode.Reliable);
                }
            }
        }
    }
}