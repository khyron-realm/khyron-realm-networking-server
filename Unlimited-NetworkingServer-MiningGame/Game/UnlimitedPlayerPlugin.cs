using System;
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

        private UnlimitedLoginPlugin _loginPlugin;

        public UnlimitedPlayerPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        /// <summary>
        ///     Player connected handler that initializes the database and sends connection confirmation to client
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            if (_loginPlugin == null)
                lock (InitializeLock)
                {
                    if (_loginPlugin == null)
                        _loginPlugin = PluginManager.GetPluginByType<UnlimitedLoginPlugin>();
                }

            using (var msg = Message.CreateEmpty(GameTags.PlayerConnected))
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
        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e) {}

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
                
                /* TO BE ACTIVATED to check the authentication
                 
                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(e.Client, GameTags.RequestFailed,
                    "Player not logged in."))
                    return;
                */
                    
                switch (message.Tag)
                {
                    case GameTags.PlayerData:
                    {
                        SendPlayerData(e);
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Sends player data to the client
        /// </summary>
        /// <param name="e">The client connected event</param>
        private void SendPlayerData(MessageReceivedEventArgs e)
        {
            // Retrieve data from database
            string id = "abc";
            ushort level = 10;
            ushort experience = 2;
            ushort energy = 7;

            var silicon = new Resource(0, "silicon", 10, 100);
            var lithium = new Resource(0, "lithium", 5, 50);
            var titanium = new Resource(0, "titanium", 20, 300);

            var newPlayerData = new PlayerData(id, level, experience, energy, silicon, lithium, titanium);

            // Send data to the client
            using (var newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayerData);

                using (var newPlayerMessage = Message.Create(GameTags.PlayerData, newPlayerWriter))
                {
                    e.Client.SendMessage(newPlayerMessage, SendMode.Reliable);
                }
            }
        }
    }
}