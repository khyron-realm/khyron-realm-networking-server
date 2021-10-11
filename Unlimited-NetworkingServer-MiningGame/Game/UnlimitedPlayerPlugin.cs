using System;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
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
                    if (_database == null)
                        _database = PluginManager.GetPluginByType<DatabaseProxy>();
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
                
                // Get client
                var client = e.Client;
                
                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, GameTags.RequestFailed, "Player not logged in.")) 
                    return;

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

                    case GameTags.CancelConversion:
                    {
                        CancelConvertResources(client);
                        break;
                    }

                    case GameTags.UpgradeRobot:
                    {
                        UpgradeRobot(client, message);
                        break;
                    }

                    case GameTags.CancelUpgrade:
                    {
                        CancelUpgradeRobot(client);
                        break;
                    }

                    case GameTags.BuildRobot:
                    {
                        BuildRobot(client, message);
                        break;
                    }

                    case GameTags.CancelBuild:
                    {
                        CancelBuildRobot(client, message);
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
            
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // Check tasks in progress and update them
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

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
                    
                    using (var msg = Message.CreateEmpty(GameTags.PlayerDataUnavailable))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            });
        }

        /// <summary>
        ///     Updates the player level
        /// </summary>
        /// <param name="level">The new player level</param>
        /// <param name="client">The connected client</param>
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
        ///     Convert the resources into energy
        /// </summary>
        /// <param name="client">The connected client</param>
        private void ConvertResources(IClient client)
        {
            Logger.Info("Converting resources");
            
            string username = GetPlayerUsername(client);
            
            // Get energy
            uint energy = 0;
            _database.DataLayer.GetPlayerEnergy(username, playerEnergy =>
            {
                energy = playerEnergy;
            });

            // Get energy threshold and conversion time
            uint energyThreshold = 0;
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
        ///     Cancels the conversion of resources
        /// </summary>
        /// <param name="client">The connected client</param>
        private void CancelConvertResources(IClient client)
        {
            Logger.Info("Cancelling resource conversion");
            
            string username = GetPlayerUsername(client);
            
            // Add resources to conversion
            _database.DataLayer.CancelResourceConversion(username, () =>
            {
                Logger.Info("Cancelling resource conversion");
            });
            
            // Send cancel conversion accepted
            using (var msg = Message.CreateEmpty(GameTags.CancelConversionAccepted))
            {
                client.SendMessage(msg, SendMode.Reliable);
            }
        }
        
        /// <summary>
        ///     Upgrade the robot part
        /// </summary>
        /// <param name="client">The connected client</param>
        private void UpgradeRobot(IClient client, Message message)
        {
            Logger.Info("Upgrading robot part");
            
            // Receive robot id and robot part
            byte robotId = 0;
            byte robotPart = 0;
            using (var reader = message.GetReader())
            {
                try
                {
                    robotId = reader.ReadByte();
                    robotPart = reader.ReadByte();
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, GameTags.RequestFailed, exception, "Failed to send required data");
                }
            }

            // Get player username
            string username = GetPlayerUsername(client);
            
            // Get energy
            uint energy = 0;
            _database.DataLayer.GetPlayerEnergy(username, playerEnergy =>
            {
                energy = playerEnergy;
            });

            // Get energy threshold and conversion time
            uint energyThreshold = 0;
            var time = DateTime.Now.AddHours(1).ToBinary();

            // Check if resources are available
            if (energy >= energyThreshold)
            {
                // Yes: Add a robot upgrade task
                _database.DataLayer.AddRobotUpgrade(username, robotId, robotPart, time, () =>
                {
                    Logger.Info("Upgrading robot part");
                });

                // Send upgrade accepted
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(time);

                    using (var msg = Message.Create(GameTags.UpgradeRobotAccepted, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            else
            {
                // No: Send upgrade rejected
                using (var msg = Message.CreateEmpty(GameTags.UpgradeRobotRejected))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        /// <summary>
        ///     Cancels the robot upgrades
        /// </summary>
        /// <param name="client">The connected client</param>
        private void CancelUpgradeRobot(IClient client)
        {
            Logger.Info("Cancelling upgrade robot");
            
            string username = GetPlayerUsername(client);

            // Add resources to conversion
            _database.DataLayer.CancelRobotUpgrade(username, () =>
            {
                Logger.Info("Cancelling robot upgrade");
            });
            
            // Send cancel conversion accepted
            using (var msg = Message.CreateEmpty(GameTags.CancelUpgradeAccepted))
            {
                client.SendMessage(msg, SendMode.Reliable);
            }
        }

        /// <summary>
        ///     Build a new robot
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The received message</param>
        private void BuildRobot(IClient client, Message message)
        {
            Logger.Info("Building robot part");
            
            // Receive robot id and robot part
            byte robotId = 0;
            using (var reader = message.GetReader())
            {
                try
                {
                    robotId = reader.ReadByte();
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, GameTags.RequestFailed, exception, "Failed to send required data");
                }
            }

            // Get player username
            string username = GetPlayerUsername(client);
            
            // Get energy
            uint energy = 0;
            _database.DataLayer.GetPlayerEnergy(username, playerEnergy =>
            {
                energy = playerEnergy;
            });

            // Get energy threshold and conversion time
            uint energyThreshold = 0;
            var time = DateTime.Now.AddHours(1).ToBinary();

            // Check if resources are available
            if (energy >= energyThreshold)
            {
                // Yes: Add a build robot task
                _database.DataLayer.AddRobotBuild(username, robotId, time, () =>
                {
                    Logger.Info("Building new robot " + robotId);
                });
                            
                // Send build accepted
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(time);

                    using (var msg = Message.Create(GameTags.BuildRobotAccepted, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            else
            {
                // No: Send build rejected
                using (var msg = Message.CreateEmpty(GameTags.BuildRobotRejected))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        private void CancelBuildRobot(IClient client, Message message)
        {
            ///
        }
        
        #endregion
        
        #region ErrorHandling

        /// <summary>
        ///     Sends an invalid data received to user
        /// </summary>
        /// <param name="client">The client where the error occured</param>
        /// <param name="tag">The error tag</param>
        /// <param name="e">The exception that occured</param>
        /// <param name="error">The error description</param>
        private void InvalidData(IClient client, ushort tag, Exception e, string error)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write((byte) 0);

                using (var msg = Message.Create(tag, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }

            Logger.Warning(error + " Invalid data received: " + e.Message + "-" + e.StackTrace);
        }

        #endregion
    }
}