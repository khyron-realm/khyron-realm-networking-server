using System;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Headquarters
{
    /// <summary>
    ///     Player manager that handles the game messages
    /// </summary>
    public class PlayerPlugin : Plugin
    {
        private static readonly object InitializeLock = new object();

        private LoginPlugin _loginPlugin;
        private DatabaseProxy _database;
        private bool _debug = true;

        public PlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
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
                        _loginPlugin = PluginManager.GetPluginByType<LoginPlugin>();
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
                        ConvertResources(client, message);
                        break;
                    }

                    case GameTags.FinishConversion:
                    {
                        FinishConvertResources(client);
                        break;
                    }

                    case GameTags.UpgradeRobot:
                    {
                        UpgradeRobot(client, message);
                        break;
                    }

                    case GameTags.FinishUpgrade:
                    {
                        FinishUpgradeRobot(client);
                        break;
                    }

                    case GameTags.BuildRobot:
                    {
                        BuildRobot(client, message);
                        break;
                    }
                    
                    case GameTags.FinishBuild:
                    {
                        FinishBuildRobot(client, message);
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
            Logger.Info("Updating player level for " + username);

            _database.DataLayer.UpdatePlayerLevel(username, level, () => { });
        }

        /// <summary>
        ///     Convert the resources into energy
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void ConvertResources(IClient client, Message message)
        {
            Logger.Info("Converting resources to energy");
            
            long startTime = 0;
            byte queueNumber = 0;
            string username = GetPlayerUsername(client);

            // Receive start time
            using (var reader = message.GetReader())
            {
                try
                {
                    startTime = reader.ReadInt64();
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, GameTags.RequestFailed, exception, "Failed to send required data");
                }
            }
            
            _database.DataLayer.TaskAvailable(username, GameConstants.ConversionTask, GameConstants.ConversionTask, isAvailable =>
            {
                if (isAvailable) 
                {
                    _database.DataLayer.GetPlayerEnergy(username, energy =>
                    {
                        // Get conversion cost
                        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        uint conversionCost = 0;

                        // Check if energy is available
                        if (energy >= conversionCost)
                        {
                            // Yes: Add resources to conversion
                            _database.DataLayer.AddResourceConversion(username, queueNumber, startTime, () => { });
                            
                            // Send conversion accepted
                            using (var msg = Message.CreateEmpty(GameTags.ConversionAccepted))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                        else
                        {
                            // No: Send conversion rejected
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 2);
                                using (var msg = Message.CreateEmpty(GameTags.ConversionRejected))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                        } 
                    });
                }
                else
                {
                    Logger.Info("Conversion task already exists");
                
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write((byte) 1);
                        using (var msg = Message.Create(GameTags.ConversionRejected, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     Finish the conversion of resources
        /// </summary>
        /// <param name="client">The connected client</param>
        private void FinishConvertResources(IClient client)
        {
            Logger.Info("Finish resource conversion");
            
            string username = GetPlayerUsername(client);
            
            _database.DataLayer.FinishResourceConversion(username, () => { });
            
            // Send finish conversion accepted
            using (var msg = Message.CreateEmpty(GameTags.FinishConversionAccepted))
            {
                client.SendMessage(msg, SendMode.Reliable);
            }
        }

        /// <summary>
        ///     Upgrade the robot part
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void UpgradeRobot(IClient client, Message message)
        {
            Logger.Info("Upgrading robot");
            
            byte robotId = 0;
            byte queueNumber = 0;
            long startTime = 0;
            string username = GetPlayerUsername(client);

            // Receive robot id and start time
            using (var reader = message.GetReader())
            {
                try
                {
                    robotId = reader.ReadByte();
                    startTime = reader.ReadInt64();
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, GameTags.RequestFailed, exception, "Failed to send required data");
                }
            }
            
            _database.DataLayer.TaskAvailable(username, queueNumber, GameConstants.UpgradeTask, isAvailable =>
            {
                if (isAvailable)
                {
                    _database.DataLayer.GetPlayerEnergy(username, energy =>
                    {
                        // Get upgrade cost
                        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        uint upgradeCost = 0;

                        // Check if energy is available
                        if (energy >= upgradeCost)
                        {
                            // Yes: Add a robot upgrade task
                            _database.DataLayer.AddRobotUpgrade(username, queueNumber, robotId, startTime, () => { });

                            // Send upgrade accepted
                            using (var msg = Message.CreateEmpty(GameTags.UpgradeRobotAccepted))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                        else
                        {
                            // No: Send upgrade rejected
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 2);
                                using (var msg = Message.CreateEmpty(GameTags.UpgradeRobotRejected))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                        }
                    });
                }
                else
                {
                    Logger.Info("Upgrade task already exists");
                
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write((byte) 1);
                        using (var msg = Message.Create(GameTags.UpgradeRobotRejected, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     Finish the robot upgrades
        /// </summary>
        /// <param name="client">The connected client</param>
        private void FinishUpgradeRobot(IClient client)
        {
            Logger.Info("Finish upgrade robot");
            
            string username = GetPlayerUsername(client);
            
            _database.DataLayer.FinishRobotUpgrade(username, () => { });
            
            // Send finish upgrade accepted
            using (var msg = Message.CreateEmpty(GameTags.FinishUpgradeAccepted))
            {
                client.SendMessage(msg, SendMode.Reliable);
            }
        }

        /// <summary>
        ///     Build a new robot
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void BuildRobot(IClient client, Message message)
        {
            Logger.Info("Building robot");
            
            byte robotId = 0;
            byte queueNumber = 0;
            long startTime = 0;
            
            // Receive the queue number, robot id and start time
            using (var reader = message.GetReader())
            {
                try
                {
                    queueNumber = reader.ReadByte();
                    robotId = reader.ReadByte();
                    startTime = reader.ReadInt64();
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, GameTags.RequestFailed, exception, "Failed to send required data");
                }
            }

            // Get player username
            string username = GetPlayerUsername(client);
            
            // Check if task already exists
            _database.DataLayer.TaskAvailable(username, queueNumber, GameConstants.BuildTask, isAvailable =>
            {
                Logger.Info("result = " + isAvailable);
                if (isAvailable)
                {
                    // Get energy
                    _database.DataLayer.GetPlayerEnergy(username, energy =>
                    {
                        // Get build cost
                        // ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------
                        uint buildCost = 0;

                        // Check if energy is available
                        if (energy >= buildCost)
                        {
                            // Yes: Add a build robot task
                            _database.DataLayer.AddRobotBuild(username, queueNumber, robotId, startTime, () => { });
                            
                            // Send build accepted
                            using (var msg = Message.CreateEmpty(GameTags.BuildRobotAccepted))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                        else
                        {
                            // No: Send build rejected
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 2);
                                using (var msg = Message.CreateEmpty(GameTags.BuildRobotRejected))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                        }
                    });
                }
                else
                {
                    Logger.Info("Build task already exists");
                
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write((byte) 1);
                        using (var msg = Message.Create(GameTags.BuildRobotRejected, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     Finish the robot upgrades
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void FinishBuildRobot(IClient client, Message message)
        {
            Logger.Info("Cancelling build robot");
            
            byte queueNumber = 0;
            string username = GetPlayerUsername(client);
            
            // Receive queue number
            using (var reader = message.GetReader())
            {
                try
                {
                    queueNumber = reader.ReadByte();
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, GameTags.RequestFailed, exception, "Failed to send required data");
                }
            }

            // Add resources to conversion
            _database.DataLayer.FinishRobotBuild(username, queueNumber,() => {});
            
            // Send cancel conversion accepted
            using (var msg = Message.CreateEmpty(GameTags.FinishBuildAccepted))
            {
                client.SendMessage(msg, SendMode.Reliable);
            }
        }
        
        /// <summary>
        ///     Cancels the robot upgrades
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void CancelBuildRobot(IClient client, Message message)
        {
            Logger.Info("Cancelling build robot");
            
            string username = GetPlayerUsername(client);
            
            // Receive robot id and robot part
            byte queueNumber = 0;
            using (var reader = message.GetReader())
            {
                try
                {
                    queueNumber = reader.ReadByte();
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, GameTags.RequestFailed, exception, "Failed to send required data");
                }
            }

            // Add resources to conversion
            _database.DataLayer.CancelRobotBuild(username, queueNumber,() => {});
            
            // Send cancel conversion accepted
            using (var msg = Message.CreateEmpty(GameTags.FinishBuildAccepted))
            {
                client.SendMessage(msg, SendMode.Reliable);
            }
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