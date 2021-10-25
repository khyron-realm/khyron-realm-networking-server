using System;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Auctions;
using Unlimited_NetworkingServer_MiningGame.Login;

namespace Unlimited_NetworkingServer_MiningGame.Chat
{
    public class ChatPlugin: Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        private static readonly object InitializeLock = new object();
        private bool _debug = true;
        private LoginPlugin _loginPlugin;
        private AuctionsPlugin _auctionsPlugin;
        
        

        public override Command[] Commands => new[]
        {
            new Command("Groups", "Show all chatgroups", "groups [username]", GetChatGroupsCommand)
        };

        public ChatPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += OnPlayerConnected;
        }

        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            if (_loginPlugin == null)
            {
                lock (InitializeLock)
                {
                    if (_loginPlugin == null)
                    {
                        _loginPlugin = PluginManager.GetPluginByType<LoginPlugin>();
                        _auctionsPlugin = PluginManager.GetPluginByType<AuctionsPlugin>();
                        //_loginPlugin.onLogout += RemovePlayerFromChatGroup;
                        //ChatGroups
                    }
                }
            }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetChatGroupsCommand(object sender, CommandEventArgs e)
        {
            
        }
    }
}