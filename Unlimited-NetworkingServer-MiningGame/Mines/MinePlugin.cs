using System;
using System.IO;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Headquarters;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Mines
{
    public class MinePlugin : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;
        
        private const string ConfigPath = @"Plugins/MinePlugin.xml";
        private LoginPlugin _loginPlugin;
        private DatabaseProxy _database;
        private bool _debug = true;

        protected override void Loaded(LoadedEventArgs args)
        {
            LoadConfig();
            if (_database == null) _database = PluginManager.GetPluginByType<DatabaseProxy>();
            if (_loginPlugin == null) _loginPlugin = PluginManager.GetPluginByType<LoginPlugin>();
        }
        
        public MinePlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }
        
        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            e.Client.MessageReceived += OnMessageReceived;
        }
        
        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
        { }
        
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag < Tags.Tags.TagsPerPlugin * Tags.Tags.Mine ||
                    message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Mine + 1)) return;

                var client = e.Client;

                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, MineTags.RequestFailed, "Player not logged in."))
                    return;

                switch (message.Tag)
                {
                    case MineTags.GetMines:
                    {
                        GetMines(client);
                        break;
                    }
                    
                    case MineTags.SaveMine:
                    {
                        SaveMine(client, message);
                        break;
                    }
                    
                    case MineTags.SaveMapPosition:
                    {
                        SaveMapPosition(client, message);
                        break;
                    }

                    case MineTags.FinishMine:
                    {
                        FinishMine(client, message);
                        break;
                    }
                }
            }
        }

        #region ReceivedCalls

        /// <summary>
        ///     Get the available mines
        /// </summary>
        /// <param name="client">The connected client</param>
        private void GetMines(IClient client)
        {
            var username = _loginPlugin.GetPlayerUsername(client);

            try
            {
                _database.DataLayer.GetMines(username, mineList =>
                {
                    using (var writer = DarkRiftWriter.Create())
                    {
                        foreach (var mine in mineList)
                        {
                            writer.Write(mine);
                        }

                        using (var msg = Message.Create(MineTags.GetMines, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 1);

                    using (var msg = Message.Create(MineTags.GetMinesFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
        }

        /// <summary>
        ///     Save the mine state to the database and update the player data
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void SaveMine(IClient client, Message message)
        {
            uint mineId;
            bool[] blocks;
            Robot[] robots;
            Resource[] resources;

            try
            {
                using (var reader = message.GetReader())
                {
                    mineId = reader.ReadUInt32();
                    blocks = reader.ReadBooleans();
                    robots = reader.ReadSerializables<Robot>();
                    resources = reader.ReadSerializables<Resource>();
                }
            }
            catch (Exception ex)
            {
                // Return Error 0 for Invalid Data Packages Received
                _loginPlugin.InvalidData(client, MineTags.RequestFailed, ex, "Save mine failed");
                return;
            }

            try
            {
                _database.DataLayer.SaveMineBlocks(mineId, blocks, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 0);

                    using (var msg = Message.Create(MineTags.SaveMineFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }

            try
            {
                _database.DataLayer.SetPlayerRobots(_loginPlugin.GetPlayerUsername(client), robots, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 1);

                    using (var msg = Message.Create(MineTags.SaveMineFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }

            try
            {
                _database.DataLayer.SetPlayerResources(_loginPlugin.GetPlayerUsername(client), resources, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 2);

                    using (var msg = Message.Create(MineTags.SaveMineFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            
            using (var msg = Message.CreateEmpty(MineTags.SaveMine))
            {
                client.SendMessage(msg, SendMode.Reliable);
            }
        }

        /// <summary>
        ///     Save the mine position in the map to the database
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void SaveMapPosition(IClient client, Message message)
        {
            uint mineId;
            byte mapPosition;

            try
            {
                using (var reader = message.GetReader())
                {
                    mineId = reader.ReadUInt32();
                    mapPosition = reader.ReadByte();
                }
            }
            catch (Exception ex)
            {
                // Return Error 0 for Invalid Data Packages Received
                _loginPlugin.InvalidData(client, MineTags.RequestFailed, ex, "Save map position failed");
                return;
            }

            try
            {
                _database.DataLayer.SaveMapPosition(mineId, mapPosition, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 0);

                    using (var msg = Message.Create(MineTags.SaveMineFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
        }
        
        /// <summary>
        ///     Remove the mine from the database and update the player data
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void FinishMine(IClient client, Message message)
        {
            uint mineId;
            Robot[] robots;
            Resource[] resources;

            try
            {
                using (var reader = message.GetReader())
                {
                    mineId = reader.ReadUInt32();
                    robots = reader.ReadSerializables<Robot>();
                    resources = reader.ReadSerializables<Resource>();
                }
            }
            catch (Exception ex)
            {
                // Return Error 0 for Invalid Data Packages Received
                _loginPlugin.InvalidData(client, MineTags.RequestFailed, ex, "Save mine failed");
                return;
            }

            try
            {
                _database.DataLayer.RemoveMine(mineId, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 0);

                    using (var msg = Message.Create(MineTags.FinishMineFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }

            try
            {
                _database.DataLayer.SetPlayerRobots(_loginPlugin.GetPlayerUsername(client), robots, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 1);

                    using (var msg = Message.Create(MineTags.FinishMineFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }

            try
            {
                _database.DataLayer.SetPlayerResources(_loginPlugin.GetPlayerUsername(client), resources, () => { });
            }
            catch
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 2);

                    using (var msg = Message.Create(MineTags.FinishMineFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            
            using (var msg = Message.CreateEmpty(MineTags.FinishMine))
            {
                client.SendMessage(msg, SendMode.Reliable);
            }
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
                    new XComment("Settings for the Mine Plugin"),
                    new XElement("Variables", new XAttribute("Debug", true))
                );
                try
                {
                    document.Save(ConfigPath);
                    if(_debug) Logger.Info("Created /Plugins/MinePlugin.xml!");
                }
                catch (Exception ex)
                {
                    if(_debug) Logger.Error("Failed to create MinePlugin.xml: " + ex.Message + " - " + ex.StackTrace);
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
                    if(_debug) Logger.Error("Failed to load MinePlugin.xml: " + ex.Message + " - " + ex.StackTrace);
                }
            }
        }

        #endregion
    }
}