using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using DarkRift;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame
{
    public class PlayerManager : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        private Dictionary<IClient, Player> players = new Dictionary<IClient, Player>();

        public PlayerManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
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
        }

        void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            
        }
    
        
    }
}