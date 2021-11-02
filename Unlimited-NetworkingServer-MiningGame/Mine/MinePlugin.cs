using System;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Mine
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
                    
                }
            }
        }
    }
}