using System;
using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame
{
    public class UnlimitedPlugin : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        private Dictionary<IClient, Player> players = new Dictionary<IClient, Player>();

        public UnlimitedPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;
        }
        
        void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            DatabaseConnector dc = new DatabaseConnector();
            
            string id = dc.GetPlayerId();
            string name = dc.getPlayerName();
            ushort level = dc.getPlayerLevel();
            ushort experience = dc.getPlayerExperience();
            ushort energy = dc.getPlayerEnergy();

            Player newPlayer = new Player(id, name, level, experience, energy);
            players.Add(e.Client, newPlayer);

            // Write player data and tell other connected clients about this player
            using (DarkRiftWriter newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayer);

                using (Message newPlayerMessage = Message.Create(Tags.PlayerConnectTag, newPlayerWriter)) {
                    e.Client.SendMessage(newPlayerMessage, SendMode.Reliable);
                }
            }
        }

        void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            players.Remove(e.Client);
        }
    
        
    }
}