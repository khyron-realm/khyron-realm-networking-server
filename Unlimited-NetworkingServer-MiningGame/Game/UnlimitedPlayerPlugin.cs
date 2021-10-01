using System;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Tags;
using Unlimited_NetworkingServer_MiningGame.Login;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    public class UnlimitedPlayerPlugin : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        public Dictionary<IClient, PlayerData> onlinePlayers = new Dictionary<IClient, PlayerData>();

        private Login.Login _loginPlugin;
        private static readonly object InitializeLock = new object();

        public UnlimitedPlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;
        }
        
        private void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            if (_loginPlugin == null)
            {
                lock (InitializeLock)
                {
                    if (_loginPlugin == null)
                    {
                        _loginPlugin = PluginManager.GetPluginByType<Login.Login>();
                    }
                }
            }

            using (var msg = Message.CreateEmpty(GameTags.PlayerConnectTag))
            {
                e.Client.SendMessage(msg, SendMode.Reliable);
            }
            
            e.Client.MessageReceived += OnMessageReceived;
        }

        private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            onlinePlayers.Remove(e.Client);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Game + 1)) 
                    return;
                
                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(e.Client, GameTags.RequestFailed, "Player not logged in.")) 
                    return;

                switch (message.Tag)
                {
                    case 0:
                    {
                        break;
                    }
                }
            }
        }
        
        private void SendPlayerData(ClientConnectedEventArgs e)
        {
            string id = "abc";
            string name = "def";
            ushort level = 10;
            ushort experience = 2;
            ushort energy = 7;

            var newPlayerData = new PlayerData(id, name, level, experience, energy);
            onlinePlayers.Add(e.Client, newPlayerData);
            
            // Send player data to the client
            using (DarkRiftWriter newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayerData);

                using (Message newPlayerMessage = Message.Create(GameTags.PlayerConnectTag, newPlayerWriter)) 
                {
                    e.Client.SendMessage(newPlayerMessage, SendMode.Reliable);
                }
            }
        }
    }
}