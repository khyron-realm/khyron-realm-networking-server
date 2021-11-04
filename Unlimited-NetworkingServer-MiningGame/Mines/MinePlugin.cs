using System;
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
        
        private LoginPlugin _loginPlugin;
        private DatabaseProxy _database;
        private bool _debug = true;

        protected override void Loaded(LoadedEventArgs args)
        {
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
            Logger.Info("Getting mines");
            
            var username = _loginPlugin.GetPlayerUsername(client);
            
            _database.DataLayer.GetMines(username, mineList =>
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    foreach (var mine in mineList)
                    {
                        writer.Write(mine);
                        Logger.Info("Sending mine " + mine.Id);
                    }
                    using (var msg = Message.Create(MineTags.GetMines, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            
                Logger.Info("Finished getting mines"); 
            });
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
        }

        #endregion
    }
}