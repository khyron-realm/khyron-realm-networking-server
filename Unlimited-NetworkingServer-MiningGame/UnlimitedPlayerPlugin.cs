using System;
using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame
{
    public class UnlimitedPlayerPlugin : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        public Dictionary<IClient, Player> onlinePlayers = new Dictionary<IClient, Player>();

        public UnlimitedPlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;
        }
        
        void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            //
            // To Be Made - Login plugin verification

            DatabaseConnector dc = new DatabaseConnector();

            string id = dc.GetPlayerId();
            string name = dc.getPlayerName();
            ushort level = dc.getPlayerLevel();
            ushort experience = dc.getPlayerExperience();
            ushort energy = dc.getPlayerEnergy();

            Player newPlayer = new Player(id, name, level, experience, energy);
            onlinePlayers.Add(e.Client, newPlayer);
            
            // Send player data to the client
            using (DarkRiftWriter newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayer);

                using (Message newPlayerMessage = Message.Create(Tags.PlayerConnectTag, newPlayerWriter)) 
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
            
        }

    }
}