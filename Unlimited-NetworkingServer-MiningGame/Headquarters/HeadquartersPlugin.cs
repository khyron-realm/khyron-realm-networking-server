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
                        FinishConvertResources(client, message);
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
                        FinishBuildRobot(client, message, true);
                        break;
                    }
                    
                    case HeadquartersTags.CancelBuild:
                    {
                        FinishBuildRobot(client, message, false);
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
            string username = GetPlayerUsername(client);
            
            if (_debug) Logger.Info("Getting data for player: " + username);
            
            _database.DataLayer.GetPlayerData(username, playerData =>
            {
                if (playerData != null)
                {
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
            string username = GetPlayerUsername(client);
            
            if (_debug) Logger.Info("Getting game data for player: " + username);
            
            _database.DataLayer.GetGameData(gameData =>
            {
                if (gameData != null)
                {
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
            string username = GetPlayerUsername(client);
            
            if (_debug) Logger.Info("Converting resources to energy for player: " + username);
            
            long startTime = 0;
            Resource[] resources = new Resource[] { };
            
            using (var reader = message.GetReader())
            {
                try
                {
                    startTime = reader.ReadInt64();
                    resources = reader.ReadSerializables<Resource>();
                }
                catch (Exception exception)
                {
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Invalid data packages received");
                }
            }

            try
            {
                _database.DataLayer.AddTask(username, 0, GameConstants.ConversionTask, 0, startTime, () => {});
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 0);
                    using (var msg = Message.Create(HeadquartersTags.ConvertResourcesError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            
            try
            {
                _database.DataLayer.SetPlayerResources(username, resources, () => {});
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 1);
                    using (var msg = Message.Create(HeadquartersTags.ConvertResourcesError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
        }

        /// <summary>
        ///     Finish the conversion of resources
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void FinishConvertResources(IClient client, Message message)
        {
            string username = GetPlayerUsername(client);
            
            if (_debug) Logger.Info("Finish resource conversion for player " + username);

            uint energy = 0;
            
            using (var reader = message.GetReader())
            {
                try
                {
                    energy = reader.ReadUInt32();
                }
                catch (Exception exception)
                {
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Invalid data packages received");
                }
            }
            
            try
            {
                _database.DataLayer.FinishTask(username, 0, GameConstants.ConversionTask, () => {});
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 0);
                    using (var msg = Message.Create(HeadquartersTags.FinishConversionError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            
            try
            {
                _database.DataLayer.SetPlayerEnergy(username, energy, () => {});
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 1);
                    using (var msg = Message.Create(HeadquartersTags.FinishConversionError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
        }

        /// <summary>
        ///     Upgrade the robot part
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void UpgradeRobot(IClient client, Message message)
        {
            string username = GetPlayerUsername(client);
            byte robotId = 0;
            long startTime = 0;
            uint energy = 0;
            
            using (var reader = message.GetReader())
            {
                try
                {
                    robotId = reader.ReadByte();
                    startTime = reader.ReadInt64();
                    energy = reader.ReadUInt32();
                }
                catch (Exception exception)
                {
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Invalid data packages received");
                }
            }
            
            Logger.Info("Upgrading robot " + robotId + " for player: " + username);
            
            try
            {
                _database.DataLayer.AddTask(username, 0, GameConstants.UpgradeTask, robotId, startTime, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 0);
                    using (var msg = Message.Create(HeadquartersTags.UpgradeRobotError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            
            try
            {
                _database.DataLayer.SetPlayerEnergy(username, energy, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 1);
                    using (var msg = Message.Create(HeadquartersTags.UpgradeRobotError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
        }

        /// <summary>
        ///     Finish the robot upgrades
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void FinishUpgradeRobot(IClient client, Message message)
        {
            string username = GetPlayerUsername(client);
            byte robotId = 0;
            Robot[] robots = new Robot[] { };
            
            using (var reader = message.GetReader())
            {
                try
                {
                    robotId = reader.ReadByte();
                    robots = reader.ReadSerializables<Robot>();
                }
                catch (Exception exception)
                {
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Invalid data packages received");
                }
            }
            
            Logger.Info("Finish upgrade robot " + robotId + " for player: " + username);
            
            try
            {
                _database.DataLayer.FinishTask(username, 0, GameConstants.UpgradeTask, () => {});
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 0);
                    using (var msg = Message.Create(HeadquartersTags.FinishUpgradeError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            
            try
            {
                _database.DataLayer.SetPlayerRobots(username, robots, () => {});
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 1);
                    using (var msg = Message.Create(HeadquartersTags.FinishUpgradeError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
        }

        /// <summary>
        ///     Build a new robot
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void BuildRobot(IClient client, Message message)
        {
            string username = GetPlayerUsername(client);
            byte robotId = 0;
            ushort queueNumber = 0;
            long startTime = 0;
            uint energy = 0;
            
            using (var reader = message.GetReader())
            {
                try
                {
                    queueNumber = reader.ReadUInt16();
                    robotId = reader.ReadByte();
                    startTime = reader.ReadInt64();
                    energy = reader.ReadUInt32();
                }
                catch (Exception exception)
                {
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Failed to send required data");
                }
            }
            
            Logger.Info("Building robot " + robotId + " with task " + queueNumber + " for player: " + username);
            
            try
            {
                _database.DataLayer.AddTask(username, queueNumber, GameConstants.BuildTask, robotId, startTime, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 0);
                    using (var msg = Message.Create(HeadquartersTags.BuildRobotError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            
            try
            {
                _database.DataLayer.SetPlayerEnergy(username, energy, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 1);
                    using (var msg = Message.Create(HeadquartersTags.BuildRobotError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
        }

        /// <summary>
        ///     Finish the robot build
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        /// <param name="isFinished">True if the task is finished and false if the task is cancelled</param>
        private void FinishBuildRobot(IClient client, Message message, bool isFinished)
        {
            string username = GetPlayerUsername(client);
            ushort queueNumber = 0;
            byte robotId = 0;
            Robot[] robots = new Robot[] { };
            uint energy = 0;

            // Receive queue number
            using (var reader = message.GetReader())
            {
                try
                {
                    queueNumber = reader.ReadUInt16();
                    robotId = reader.ReadByte();
                    if (isFinished)
                    {
                        robots = reader.ReadSerializables<Robot>();
                    }
                    else
                    {
                        energy = reader.ReadUInt32();
                    }
                }
                catch (Exception exception)
                {
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Invalid data packages received");
                }
            }

            Logger.Info((isFinished ? "Finished" : "Cancelled") + " build robot " + robotId + " with task " +
                        queueNumber + " for player: " + username);

            try
            {
                _database.DataLayer.FinishTask(username, queueNumber, GameConstants.BuildTask, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 0);
                    using (var msg = Message.Create(HeadquartersTags.FinishBuildError, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }

            if (isFinished)
            {
                try
                {
                    _database.DataLayer.SetPlayerRobots(username, robots, () => { });
                }
                catch
                {
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write((byte) 1);
                        using (var msg = Message.Create(HeadquartersTags.FinishBuildError, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }
            }
            else
            {
                try
                {
                    _database.DataLayer.SetPlayerEnergy(username, energy, () => { });
                }
                catch
                {
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write((byte) 1);
                        using (var msg = Message.Create(HeadquartersTags.CancelBuildError, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }
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