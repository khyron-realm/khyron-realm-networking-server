using System;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Game;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Headquarters
{
    /// <summary>
    ///     Player manager that handles the game messages
    /// </summary>
    public class HeadquartersPlugin : Plugin
    {
        private static readonly object InitializeLock = new object();

        private LoginPlugin _loginPlugin;
        private DatabaseProxy _database;
        private GameData _gameData;
        private bool _debug = true;

        public HeadquartersPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
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

            _gameData = new GameData();
            
            using (var msg = Message.CreateEmpty(HeadquartersTags.PlayerConnected))
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
                if (message.Tag < Tags.Tags.TagsPerPlugin * Tags.Tags.Headquarters ||
                    message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Headquarters + 1)) return;

                // Get client
                var client = e.Client;

                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, HeadquartersTags.RequestFailed, "Player not logged in.")) 
                    return;

                switch (message.Tag)
                {
                    case HeadquartersTags.PlayerData:
                    {
                        SendPlayerData(client);
                        break;
                    }

                    case HeadquartersTags.GameData:
                    {
                        SendGameData(client);
                        break;
                    }
                    
                    case HeadquartersTags.ConvertResources:
                    {
                        ConvertResources(client, message);
                        break;
                    }

                    case HeadquartersTags.FinishConversion:
                    {
                        FinishConvertResources(client);
                        break;
                    }

                    case HeadquartersTags.UpgradeRobot:
                    {
                        UpgradeRobot(client, message);
                        break;
                    }

                    case HeadquartersTags.FinishUpgrade:
                    {
                        FinishUpgradeRobot(client, message);
                        break;
                    }

                    case HeadquartersTags.BuildRobot:
                    {
                        BuildRobot(client, message);
                        break;
                    }
                    
                    case HeadquartersTags.FinishBuild:
                    {
                        FinishBuildRobot(client, message, true, true);
                        break;
                    }
                    
                    case HeadquartersTags.CancelInProgressBuild:
                    {
                        FinishBuildRobot(client, message, false, true);
                        break;
                    }
                    
                    case HeadquartersTags.CancelOnHoldBuild:
                    {
                        FinishBuildRobot(client, message, false, false);
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

                        using (var newPlayerMessage = Message.Create(HeadquartersTags.PlayerData, newPlayerWriter))
                        {
                            client.SendMessage(newPlayerMessage, SendMode.Reliable);
                        }
                    }
                }
                else
                {
                    if (_debug) Logger.Info("Player data is not available for user " + username);
                    
                    using (var msg = Message.CreateEmpty(HeadquartersTags.PlayerDataUnavailable))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            });
        }
        
        /// <summary>
        ///     Sends game data to the client
        /// </summary>
        /// <param name="client">The connected client</param>
        private void SendGameData(IClient client)
        {
            Logger.Info("Getting game data");
            
            // Retrieve data from database
            _database.DataLayer.GetGameData(gameData =>
            {
                if (gameData != null)
                {
                    _gameData = gameData;
                    
                    // Send data to the client
                    using (var newPlayerWriter = DarkRiftWriter.Create())
                    {
                        newPlayerWriter.Write(gameData);

                        using (var newPlayerMessage = Message.Create(HeadquartersTags.GameData, newPlayerWriter))
                        {
                            client.SendMessage(newPlayerMessage, SendMode.Reliable);
                        }
                    }
                }
                else
                {
                    if (_debug) Logger.Info("Game data is not available for user " + GetPlayerUsername(client));
                    
                    using (var msg = Message.CreateEmpty(HeadquartersTags.GameDataUnavailable))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            });
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
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Failed to send required data");
                }
            }
            
            _database.DataLayer.TaskAvailable(username, GameConstants.ConversionTask, GameConstants.ConversionTask, isAvailable =>
            {
                if (isAvailable) 
                {
                    _database.DataLayer.GetPlayerResources(username, resources =>
                    {
                        Logger.Info("Getting player resources");
                        // Get conversion cost
                        bool validCondition = true;
                        foreach (var tuple in resources.Zip(_gameData.Resources, (x, y) => (x, y)))
                        {
                            if (tuple.x.Count < tuple.y.ConversionRate)
                            {
                                validCondition = false;
                            }
                        }
                        
                        if (validCondition)
                        {
                            // Yes: Add resources to conversion
                            _database.DataLayer.AddTask(username, 0, GameConstants.ConversionTask, 0, startTime,
                                () =>
                                {
                                    // Decrease resources count
                                    foreach (var tuple in resources.Zip(_gameData.Resources, (x, y) => (x, y)))
                                    {
                                        tuple.x.Count -= tuple.y.ConversionRate;
                                    }
                                    _database.DataLayer.SetPlayerResources(username, resources, () => {});
                                    
                                    // Send conversion accepted
                                    using (var msg = Message.CreateEmpty(HeadquartersTags.ConversionAccepted))
                                    {
                                        client.SendMessage(msg, SendMode.Reliable);
                                    }
                                    
                                    // Send new resources count
                                    using (var writer = DarkRiftWriter.Create())
                                    {
                                        writer.Write(resources);
                                        using (var msg = Message.Create(HeadquartersTags.ResourcesUpdate, writer))
                                        {
                                            client.SendMessage(msg, SendMode.Reliable);
                                        }
                                    }
                                });
                        }
                        else
                        {
                            // No: Send conversion rejected
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 2);
                                using (var msg = Message.Create(HeadquartersTags.ConversionRejected, writer))
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
                        using (var msg = Message.Create(HeadquartersTags.ConversionRejected, writer))
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
            
            _database.DataLayer.FinishTask(username, 0, GameConstants.ConversionTask, () =>
            {
                // Increase energy count
                _database.DataLayer.GetPlayerEnergy(username, energy =>
                {
                    energy += 10000;
                    
                    // Send finish conversion accepted
                    using (var msg = Message.CreateEmpty(HeadquartersTags.FinishConversionAccepted))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                    
                    // Send new energy count
                    _database.DataLayer.SetPlayerEnergy(username, energy, () =>
                    {
                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(energy);
                            using (var msg = Message.Create(HeadquartersTags.EnergyUpdate, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                    });
                });
            });
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
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Failed to send required data");
                }
            }
            
            _database.DataLayer.TaskAvailable(username, 0, GameConstants.UpgradeTask, isAvailable =>
            {
                if (isAvailable)
                {
                    _database.DataLayer.GetPlayerEnergy(username, energy =>
                    {
                        // Get upgrade cost
                        uint upgradeCost = _gameData.Robots[robotId].UpgradePrice;

                        // Check if energy is available
                        if (energy >= upgradeCost)
                        {
                            // Yes: Add a robot upgrade task
                            _database.DataLayer.AddTask(username, 0, GameConstants.UpgradeTask, robotId, 
                                startTime, () =>
                                {
                                    // Decrease energy
                                    energy -= upgradeCost;
                                    _database.DataLayer.SetPlayerEnergy(username, energy, () =>
                                    {
                                        // Send upgrade accepted
                                        using (var msg = Message.CreateEmpty(HeadquartersTags.UpgradeRobotAccepted))
                                        {
                                            client.SendMessage(msg, SendMode.Reliable);
                                        }
                                        
                                        // Send new energy
                                        using (var writer = DarkRiftWriter.Create())
                                        {
                                            writer.Write(energy);
                                            using (var msg = Message.Create(HeadquartersTags.EnergyUpdate, writer))
                                            {
                                                client.SendMessage(msg, SendMode.Reliable);
                                            }
                                        }
                                    });
                                    
                                });
                        }
                        else
                        {
                            // No: Send upgrade rejected
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 2);
                                using (var msg = Message.Create(HeadquartersTags.UpgradeRobotRejected, writer))
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
                        using (var msg = Message.Create(HeadquartersTags.UpgradeRobotRejected, writer))
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
        private void FinishUpgradeRobot(IClient client, Message message)
        {
            Logger.Info("Finish upgrade robot");
            
            byte robotId = 0;
            string username = GetPlayerUsername(client);

            // Receive robot id and start time
            using (var reader = message.GetReader())
            {
                try
                {
                    robotId = reader.ReadByte();
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Failed to send required data");
                }
            }
            
            _database.DataLayer.FinishTask(username, 0, GameConstants.UpgradeTask, () =>
            {
                
                _database.DataLayer.GetPlayerRobots(username, robots =>
                {
                    // Increase robot level
                    robots[robotId].Level++;
                    
                    _database.DataLayer.SetPlayerRobots(username, robots, () =>
                    {
                        // Send finish upgrade accepted
                        using (var msg = Message.CreateEmpty(HeadquartersTags.FinishUpgradeAccepted))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                
                        // Send new robots
                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(robots);
                            using (var msg = Message.Create(HeadquartersTags.RobotsUpdate, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                    });
                });
            });
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
            ushort queueNumber = 0;
            long startTime = 0;
            
            // Receive the queue number, robot id and start time
            using (var reader = message.GetReader())
            {
                try
                {
                    queueNumber = reader.ReadUInt16();
                    robotId = reader.ReadByte();
                    startTime = reader.ReadInt64();
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Failed to send required data");
                }
            }

            // Get player username
            string username = GetPlayerUsername(client);
            
            Logger.Info("building robot " + queueNumber + " - " + robotId + " - " + DateTime.FromBinary(startTime));
            
            // Check if task already exists
            _database.DataLayer.TaskAvailable(username, queueNumber, GameConstants.BuildTask, isAvailable =>
            {
                Logger.Info("Task available " + isAvailable);
                if (isAvailable)
                {
                    // Get energy
                    _database.DataLayer.GetPlayerEnergy(username, energy =>
                    {
                        // Get build cost
                        uint buildCost = _gameData.Robots[robotId].BuildPrice;

                        // Check if energy is available
                        if (energy >= buildCost)
                        {
                            // Yes: Add a build robot task
                            _database.DataLayer.AddTask(username, queueNumber, GameConstants.BuildTask, robotId,
                                startTime, () =>
                                {
                                    // Decrease energy
                                    energy -= buildCost;
                                    _database.DataLayer.SetPlayerEnergy(username, energy, () =>
                                    {
                                        // Send build accepted
                                        using (var msg = Message.CreateEmpty(HeadquartersTags.BuildRobotAccepted))
                                        {
                                            client.SendMessage(msg, SendMode.Reliable);
                                        }
                                        
                                        // Send new energy
                                        using (var writer = DarkRiftWriter.Create())
                                        {
                                            writer.Write(energy);
                                            using (var msg = Message.Create(HeadquartersTags.EnergyUpdate, writer))
                                            {
                                                client.SendMessage(msg, SendMode.Reliable);
                                            }
                                        }
                                    });
                                });
                        }
                        else
                        {
                            // No: Send build rejected
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 2);
                                using (var msg = Message.Create(HeadquartersTags.BuildRobotRejected, writer))
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
                        using (var msg = Message.Create(HeadquartersTags.BuildRobotRejected, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     Finish the robot build
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        /// <param name="isFinished">True if the task is finished and false if the task is cancelled</param>
        /// <param name="inProgress">True if the task is in progress or false otherwise</param>
        private void FinishBuildRobot(IClient client, Message message, bool isFinished, bool inProgress = true)
        {
            Logger.Info("Finish build robot");
            
            ushort queueNumber = 0;
            byte robotId = 0;
            long startTime = 0;
            string username = GetPlayerUsername(client);
            
            // Receive queue number
            using (var reader = message.GetReader())
            {
                try
                {
                    queueNumber = reader.ReadUInt16();
                    robotId = reader.ReadByte();
                    startTime = reader.ReadInt64();
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Failed to send required data");
                }
            }
            
            Logger.Info("Finish build robot with " + queueNumber + " - " + startTime);
            
            _database.DataLayer.FinishTask(username, queueNumber,GameConstants.BuildTask, () =>
            {
                if (isFinished)
                {
                    // Task finished -> increase robot type count
                    _database.DataLayer.GetPlayerRobots(username, robots =>
                    {
                        robots[robotId].Count++;
                        
                        _database.DataLayer.UpdateTasks(username, queueNumber, GameConstants.BuildTask, startTime, () =>
                        {
                            _database.DataLayer.SetPlayerRobots(username, robots, () =>
                            {
                                // Send finish task accepted
                                using (var msg = Message.CreateEmpty(HeadquartersTags.FinishBuildAccepted))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                    Logger.Info("Sending finish build accepted");
                                }
                        
                                // Send new robots
                                using (var writer = DarkRiftWriter.Create())
                                {
                                    writer.Write(robots);
                                    using (var msg = Message.Create(HeadquartersTags.RobotsUpdate, writer))
                                    {
                                        client.SendMessage(msg, SendMode.Reliable);
                                    }
                                }
                            });
                        });
                    });
                }
                else
                {
                    // Task cancelled -> return energy
                    // Decrease energy
                    _database.DataLayer.GetPlayerEnergy(username, energy =>
                    {
                        // Get upgrade cost
                        energy += _gameData.Robots[robotId].BuildPrice;
                        
                        _database.DataLayer.SetPlayerEnergy(username, energy, () =>
                        {
                            if (inProgress)
                            {   
                                _database.DataLayer.UpdateTasks(username, queueNumber, GameConstants.BuildTask, startTime, () =>
                                {
                                    // Send cancel task accepted
                                    using (var writer = DarkRiftWriter.Create())
                                    {
                                        writer.Write((byte)0);
                                        using (var msg = Message.Create(HeadquartersTags.CancelBuildAccepted, writer))
                                        {
                                            client.SendMessage(msg, SendMode.Reliable);
                                            Logger.Info("Sending finish build accepted");
                                        }
                                    }
                                });
                            }
                            else
                            {
                                // Send cancel task accepted
                                using (var writer = DarkRiftWriter.Create())
                                {
                                    writer.Write((byte)1);
                                    using (var msg = Message.Create(HeadquartersTags.CancelBuildAccepted, writer))
                                    {
                                        client.SendMessage(msg, SendMode.Reliable);
                                        Logger.Info("Sending finish build accepted");
                                    }
                                }
                            }
                    
                            // Send new energy
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write(energy);
                                using (var msg = Message.Create(HeadquartersTags.EnergyUpdate, writer))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                        });
                    });
                } 
            });
        }

        #endregion

        #region PlayerDataMethods
        
        

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