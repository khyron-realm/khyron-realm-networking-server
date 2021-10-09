using System;
using System.Diagnostics;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.GameElements;
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
        private DatabaseProxy _database;
        private bool _debug = true;

        public UnlimitedPlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        private string GetPlayerUsername(IClient client)
        {
            return _loginPlugin.GetPlayerUsername(client);
        }
        
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
            if (_database == null)
                lock (InitializeLock)
                {
                    if (_database == null) _database = PluginManager.GetPluginByType<DatabaseProxy>();
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

                // TO BE ACTIVATED to check the authentication
                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, GameTags.RequestFailed, "Player not logged in.")) return;

                switch (message.Tag)
                {
                    case GameTags.PlayerData:
                    {
                        SendPlayerData(client);
                        break;
                    }

                    case GameTags.ConvertResources:
                    {
                        ConvertResources(client);
                        break;
                    }

                    case GameTags.UpgradeRobot:
                    {
                        UpgradeRobot(client);
                        break;
                    }

                    case GameTags.BuildRobot:
                    {
                        BuildRobot(client);
                        break;
                    }
                }
            }
        }

        #region ReceivedCalls
        
        /// <summary>
        ///     Sends player data to the client
        /// </summary>
        /// <param name="client">The connected client</param>
        private void SendPlayerData(IClient client)
        {
            Logger.Info("Getting player data");
            string username = GetPlayerUsername(client);

            // Retrieve data from database
            _database.DataLayer.GetPlayerData(username, playerData =>
            {
                if (playerData != null)
                {
                    // Send data to the client
                    using (var newPlayerWriter = DarkRiftWriter.Create())
                    {
                        newPlayerWriter.Write(playerData);

                        using (var newPlayerMessage = Message.Create(GameTags.PlayerData, newPlayerWriter))
                        {
                            client.SendMessage(newPlayerMessage, SendMode.Reliable);
                        }
                    }
                }
                else
                {
                    if (_debug) Logger.Info("Player data is not available for user " + username);
                    
                    // TO ADD - send error message to the user
                }
            });
        }

        private void UpdatePlayerLevel(byte level, IClient client)
        {
            string username = GetPlayerUsername(client);
            Logger.Info("Updating username: " + username);

            _database.DataLayer.UpdatePlayerLevel(username, level, () =>
            {
                Logger.Info("Updated player level");
            });
        }
        
        /// <summary>
        /// Convert the resources into energy
        /// </summary>
        /// <param name="client"></param>
        private void ConvertResources(IClient client)
        {
            Logger.Info("Converting resources");
            
            string username = GetPlayerUsername(client);
            uint energy = 10;
            uint energyThreshold = 1;
            _database.DataLayer.GetPlayerEnergy(username, playerEnergy =>
            {
                energy = playerEnergy;
            });

            var time = DateTime.Now.AddHours(1).ToBinary();

            // Check if resources are available
            if (energy >= energyThreshold)
            {
                // Yes: Add resources to conversion
                _database.DataLayer.AddResourceConversion(username, time, () =>
                {
                    Logger.Info("Converting resources to energy");
                });
                            
                // Send conversion accepted
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(time);

                    using (var msg = Message.Create(GameTags.ConversionAccepted, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            else
            {
                // No: Send conversion rejected
                using (var msg = Message.CreateEmpty(GameTags.ConversionRejected))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }
        
        /// <summary>
        /// Upgrade the robot part
        /// </summary>
        /// <param name="client"></param>
        private void UpgradeRobot(IClient client)
        {
        }

        /// <summary>
        /// Build a new robot
        /// </summary>
        /// <param name="client"></param>
        private void BuildRobot(IClient client)
        {
        }

        #endregion
    }
}