using System;
using System.IO;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Headquarters
{
    /// <summary>
    ///     Player manager that handles the headquarter messages
    /// </summary>
    public class HeadquartersPlugin : Plugin
    {
        private const string ConfigPath = @"Plugins/HeadquartersPlugin.xml";
        private LoginPlugin _loginPlugin;
        private DatabaseProxy _database;
        private bool _debug = true;

        protected override void Loaded(LoadedEventArgs args)
        {
            if (_database == null) _database = PluginManager.GetPluginByType<DatabaseProxy>();
            if (_loginPlugin == null) _loginPlugin = PluginManager.GetPluginByType<LoginPlugin>();
        }
        
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
                        SendGameData(client, message);
                        break;
                    }
                    
                    case HeadquartersTags.UpdateLevel:
                    {
                        UpdateLevel(client, message);
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
                        FinishBuildRobot(client, message, false, true);
                        break;
                    }

                    case HeadquartersTags.FinishBuildMultiple:
                    {
                        FinishBuildRobot(client, message, true, true);
                        break;
                    }
                    
                    case HeadquartersTags.CancelInProgressBuild:
                    {
                        FinishBuildRobot(client, message, false, false);
                        break;
                    }
                    
                    case HeadquartersTags.CancelOnHoldBuild:
                    {
                        FinishBuildRobot(client, message, false, false, false);
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
                    if (_debug) Logger.Warning("Player data is not available for user " + username);
                    
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
        /// <param name="message">The message received</param>
        private void SendGameData(IClient client, Message message)
        {
            string username = GetPlayerUsername(client);
            
            ushort version = 0;

            using (var reader = message.GetReader())
            {
                try
                {
                    version = reader.ReadUInt16();
                }
                catch (Exception exception)
                {
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Invalid data packages received");
                }
            }
            
            _database.DataLayer.GetGameData(gameData =>
            {
                if (gameData != null)
                {
                    if (version < gameData.Version)
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
                }
                else
                {
                    if (_debug) Logger.Warning("Game data is not available for user " + GetPlayerUsername(client));
                    
                    using (var msg = Message.CreateEmpty(HeadquartersTags.GameDataUnavailable))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            });
        }
        
        /// <summary>
        ///     Updates the player level and remaining experience
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void UpdateLevel(IClient client, Message message)
        {
            string username = GetPlayerUsername(client);
            
            byte level = 0;
            uint experience = 0;

            using (var reader = message.GetReader())
            {
                try
                {
                    level = reader.ReadByte();
                    experience = reader.ReadUInt32();
                }
                catch (Exception exception)
                {
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Invalid data packages received");
                }
            }

            try
            {
                _database.DataLayer.SetPlayerLevelExperience(username, level, experience, () => { });
            }
            catch
            {
                if (_debug) Logger.Warning("Update level error for user " + GetPlayerUsername(client));
                    
                using (var msg = Message.CreateEmpty(HeadquartersTags.UpdateLevelError))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        /// <summary>
        ///     Convert the resources into energy
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void ConvertResources(IClient client, Message message)
        {
            string username = GetPlayerUsername(client);
            
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
                _database.DataLayer.AddTask(TaskType.Conversion, username, 0, 0, startTime, () => {});
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
            
            uint energy = 0;
            uint experience = 0;

            using (var reader = message.GetReader())
            {
                try
                {
                    energy = reader.ReadUInt32();
                    experience = reader.ReadUInt32();
                }
                catch (Exception exception)
                {
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Invalid data packages received");
                }
            }
            
            try
            {
                _database.DataLayer.FinishTask(TaskType.Conversion, username, 0, () => {});
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
                _database.DataLayer.SetPlayerExperience(username, experience, () => {});
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
            
            try
            {
                _database.DataLayer.AddTask(TaskType.Upgrade, username, 0, robotId, startTime, () => { });
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
            Robot robot = new Robot();
            uint experience = 0;
            
            using (var reader = message.GetReader())
            {
                try
                {
                    robotId = reader.ReadByte();
                    robot = reader.ReadSerializable<Robot>();
                    experience = reader.ReadUInt32();
                }
                catch (Exception exception)
                {
                    InvalidData(client, HeadquartersTags.RequestFailed, exception, "Invalid data packages received");
                }
            }
            
            try
            {
                _database.DataLayer.FinishTask(TaskType.Upgrade, username, 0, () => {});
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
                _database.DataLayer.SetPlayerRobot(username, robotId, robot, () => {});
                _database.DataLayer.SetPlayerExperience(username, experience, () => {});
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
            ushort queueNumber = 0;
            byte robotId = 0;
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
            
            try
            {
                _database.DataLayer.AddTask(TaskType.Build, username, queueNumber, robotId, startTime, () => { });
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
        /// <param name="multipleRobots">True if multiple robots are updated and false if only a single robot is updated</param>
        /// <param name="isFinished">True if the task is finished and false if the task is cancelled</param>
        /// <param name="inProgress">True if the task is in progress or false otherwise</param>
        private void FinishBuildRobot(IClient client, Message message, bool multipleRobots, bool isFinished, bool inProgress = true)
        {
            string username = GetPlayerUsername(client);
            ushort queueNumber = 0;
            byte robotId = 0;
            long startTime = 0;
            Robot robot = new Robot();
            Robot[] robots = new Robot[] {};
            uint energy = 0;

            // Receive queue number
            using (var reader = message.GetReader())
            {
                try
                {
                    queueNumber = reader.ReadUInt16();
                    robotId = reader.ReadByte();
                    startTime = reader.ReadInt64();
                    if (isFinished)
                    {
                        if(!multipleRobots) robot = reader.ReadSerializable<Robot>();
                        else robots = reader.ReadSerializables<Robot>();
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

            try
            {
                _database.DataLayer.FinishTask(TaskType.Build, username, queueNumber, () => { });
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
                    _database.DataLayer.UpdateNextTask(TaskType.Build, username, queueNumber, startTime, () => {});
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
                try
                {
                    if(!multipleRobots) _database.DataLayer.SetPlayerRobot(username, robotId, robot, () => { });
                    else _database.DataLayer.SetPlayerRobots(username, robots, () => { });
                }
                catch
                {
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write((byte) 2);
                        using (var msg = Message.Create(HeadquartersTags.FinishBuildError, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }
            }
            else
            {
                if (inProgress)
                {
                    try
                    {
                        _database.DataLayer.UpdateNextTask(TaskType.Build, username, queueNumber, startTime, () => {});
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
                try
                {
                    _database.DataLayer.SetPlayerEnergy(username, energy, () => { });
                }
                catch
                {
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write((byte) 2);
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

            if(_debug) Logger.Warning(error + " Invalid data received: " + e.Message + "-" + e.StackTrace);
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     Load the configuration file
        /// </summary>
        private void LoadConfig()
        {
            XDocument document;

            if (!File.Exists(ConfigPath))
            {
                document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("Settings for the Headquarters Plugin"),
                    new XElement("Variables", new XAttribute("Debug", true))
                );
                try
                {
                    document.Save(ConfigPath);
                    if(_debug) Logger.Info("Created /Plugins/HeadquartersPlugin.xml!");
                }
                catch (Exception ex)
                {
                    if(_debug) Logger.Error("Failed to create HeadquartersPlugin.xml: " + ex.Message + " - " + ex.StackTrace);
                }
            }
            else
            {
                try
                {
                    document = XDocument.Load(ConfigPath);
                    _debug = document.Element("Variables").Attribute("Debug").Value == "true";
                }
                catch (Exception ex)
                {
                    if(_debug) Logger.Error("Failed to load HeadquartersPlugin.xml: " + ex.Message + " - " + ex.StackTrace);
                }
            }
        }

        #endregion
    }
}