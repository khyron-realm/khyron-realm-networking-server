using System;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    /// <summary>
    ///     Player manager that handles the game messages
    /// </summary>
    public class UnlimitedPlayerPlugin : Plugin
    {
        private static readonly object InitializeLock = new object();

        private UnlimitedLoginPlugin _unlimitedLoginPluginPlugin;

        public Dictionary<IClient, PlayerData> onlinePlayers = new Dictionary<IClient, PlayerData>();

        public UnlimitedPlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += ClientConnected;
            ClientManager.ClientDisconnected += ClientDisconnected;
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        /// <summary>
        ///     Player connected handler that initializes the database and sends connection confirmation to client
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            if (_unlimitedLoginPluginPlugin == null)
                lock (InitializeLock)
                {
                    if (_unlimitedLoginPluginPlugin == null)
                        _unlimitedLoginPluginPlugin = PluginManager.GetPluginByType<UnlimitedLoginPlugin>();
                }

            using (var msg = Message.CreateEmpty(GameTags.PlayerConnectTag))
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
        private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            onlinePlayers.Remove(e.Client);
        }

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
                if (message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Game + 1))
                    return;

                // If player isn't logged in, return error 1
                if (!_unlimitedLoginPluginPlugin.PlayerLoggedIn(e.Client, GameTags.RequestFailed,
                    "Player not logged in."))
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

        /// <summary>
        ///     [NOT COMPLETED] Sends player data to the client
        /// </summary>
        /// <param name="e">The client connected event</param>
        private void SendPlayerData(ClientConnectedEventArgs e)
        {
            var id = "abc";
            var name = "def";
            ushort level = 10;
            ushort experience = 2;
            ushort energy = 7;

            var newPlayerData = new PlayerData(id, name, level, experience, energy);
            onlinePlayers.Add(e.Client, newPlayerData);

            // Send player data to the client
            using (var newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayerData);

                using (var newPlayerMessage = Message.Create(GameTags.PlayerConnectTag, newPlayerWriter))
                {
                    e.Client.SendMessage(newPlayerMessage, SendMode.Reliable);
                }
            }
        }
    }
}