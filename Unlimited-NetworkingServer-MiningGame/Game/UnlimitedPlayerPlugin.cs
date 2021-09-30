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

        public Dictionary<IClient, Player> onlinePlayers = new Dictionary<IClient, Player>();

        private Login.Login _loginPlugin;

        public UnlimitedPlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;
        }
        
        void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            //
            // To Be Made - Login plugin verification
            
            // If player isn't logged in, return error 1
            if (!_loginPlugin.PlayerLoggedIn(e.Client, GameTags.RequestFailed, "Player not logged in."))
            {
                return;
            }

            string id = " ";
            string name = " ";
            ushort level = 0;
            ushort experience = 0;
            ushort energy = 0;

            Player newPlayer = new Player(id, name, level, experience, energy);
            onlinePlayers.Add(e.Client, newPlayer);
            
            // Send player data to the client
            using (DarkRiftWriter newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayer);

                using (Message newPlayerMessage = Message.Create(GameTags.PlayerConnectTag, newPlayerWriter)) 
                {
                    e.Client.SendMessage(newPlayerMessage, SendMode.Reliable);
                }
            }
            
            e.Client.MessageReceived += OnMessageReceived;
        }

        void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            onlinePlayers.Remove(e.Client);
        }

        void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Game + 1))
                {
                    return;
                }

                var client = e.Client;

                switch (message.Tag)
                {
                    case 0:
                    {
                        break;
                    }
                }
            }
        }
    }
}