using System;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.GameElements;
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
                
                var client = e.Client;
                
                /* TO BE ACTIVATED to check the authentication
                 
                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, GameTags.RequestFailed, "Player not logged in.")) return;
                */
                    
                switch (message.Tag)
                {
                    case GameTags.PlayerData:
                    {
                        SendPlayerData(client);
                        break;
                    }

                    case GameTags.ConvertResources:
                    {
                        ConvertResources(client);
                        break;
                    }

                    case GameTags.UpgradeRobot:
                    {
                        UpgradeRobot(client);
                        break;
                    }

                    case GameTags.BuildRobot:
                    {
                        BuildRobot(client);
                        break;
                    }
                }
            }
        }

        #region ReceivedCalls
        
        /// <summary>
        ///     Sends player data to the client
        /// </summary>
        /// <param name="client">The connected client</param>
        private void SendPlayerData(IClient client)
        {
            // Retrieve data from database
            
            // Create player data
            string id = "abc";
            byte level = 10;
            ushort experience = 2;
            uint energy = 7;

            byte nrRobots = 3;
            byte nrResources = 3;
            byte nrBuildTasks = 3;

            // Create resources
            Resource[] resources = new Resource[nrResources];
            foreach (int iterator in Enumerable.Range(0, nrResources))
            {
                resources[iterator] = new Resource(0, "silicon", 10, 100);
            }

            // Create robots
            Robot[] robots = new Robot[nrRobots];
            foreach (int iterator in Enumerable.Range(0, nrRobots))
            {
                robots[iterator] = new Robot(0, "worker", 1, 1, 1, 1);
            }
            
            // Create resource conversion task
            var time = DateTime.Now.ToBinary();
            BuildTask resourceConversion = new BuildTask(0, 0, 0, time);
            
            // Create robot upgrading tasks
            time = DateTime.Now.ToBinary();
            BuildTask robotUpgrading = new BuildTask(0, 0, 0, time);
            
            // Create robot building tasks
            time = DateTime.Now.ToBinary();
            BuildTask[] robotBuilding = new BuildTask[nrBuildTasks];
            foreach (int iterator in Enumerable.Range(0, nrResources))
            {
                robotBuilding[iterator] = new BuildTask(0, 0, 0, time);
            }

            // Create player object
            var newPlayerData = new PlayerData(id, level, experience, energy, resources, robots, resourceConversion, robotUpgrading, robotBuilding);

            // Send data to the client
            using (var newPlayerWriter = DarkRiftWriter.Create())
            {
                newPlayerWriter.Write(newPlayerData);

                using (var newPlayerMessage = Message.Create(GameTags.PlayerData, newPlayerWriter))
                {
                    client.SendMessage(newPlayerMessage, SendMode.Reliable);
                }
            }
        }
        
        /// <summary>
        /// Convert the resources into energy
        /// </summary>
        /// <param name="client"></param>
        private void ConvertResources(IClient client)
        {
            bool conversionResult = true;
            Logger.Info("Converting resources");
                        
            // Check if resources are available
            if (conversionResult == true)
            {
                // Yes: Add resources to conversion
                            
                // Send conversion accepted
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(DateTime.Now.ToBinary());

                    using (var msg = Message.Create(GameTags.ConversionAccepted, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            else
            {
                // No: Send conversion rejected
                using (var msg = Message.CreateEmpty(GameTags.ConversionRejected))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }
        
        /// <summary>
        /// Upgrade the robot part
        /// </summary>
        /// <param name="client"></param>
        private void UpgradeRobot(IClient client)
        {
        }

        /// <summary>
        /// Build a new robot
        /// </summary>
        /// <param name="client"></param>
        private void BuildRobot(IClient client)
        {
        }

        #endregion
    }
}