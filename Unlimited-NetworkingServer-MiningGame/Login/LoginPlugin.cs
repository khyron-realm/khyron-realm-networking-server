using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Headquarters;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Login
{
    /// <summary>
    ///     Login Manager for handling authentication
    /// </summary>
    public class LoginPlugin : Plugin
    {
        private const string PrivateKeyPath = @"Plugins/PrivateKey.xml";
        private static readonly object InitializeLock = new object();

        private ConcurrentDictionary<string, IClient> _clients = new ConcurrentDictionary<string, IClient>();

        private ConcurrentDictionary<IClient, string> _usersLoggedIn = new ConcurrentDictionary<IClient, string>();

        private bool _allowAddUser = true;
        private DatabaseProxy _database;
        private bool _debug = true;
        private string _privateKey;

        public LoginPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            LoadRsaKey();

            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        public override Command[] Commands => new[]
        {
            new Command("AllowAddUser", "Allow users to be added to the Database", "AllowAddUser [on/off]",
                AllowAddUserCommand),
            new Command("AddUser", "Adds a User to the Database", "AddUser -username -password", AddUserCommand),
            new Command("DellUser", "Deletes a User from the Database", "DellUser -username", DellUserCommand),
            new Command("OnlineUsers", "Logs the number of online users", "OnlineUsers", OnlineUsersCommand),
            new Command("LoggedInUsers", "Logs the number of logged in users", "LoggedInUsers", LoggedInUsersCommand),
            new Command("LPDebug", "Enables the debug logs for the Login Plugin", "LPDebug [on/off]", LpDebugCommand)
        };

        /// <summary>
        ///     Loads the RSA private key string
        /// </summary>
        private void LoadRsaKey()
        {
            try
            {
                _privateKey = File.ReadAllText(PrivateKeyPath);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Failed to load PrivateKey.xml: " + ex.Message + " - " + ex.StackTrace);
            }
        }

        /// <summary>
        ///     Player connected handler that initializes the database and updates the logged in users
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            if (_database == null)
                lock (InitializeLock)
                {
                    if (_database == null) _database = PluginManager.GetPluginByType<DatabaseProxy>();
                }

            _usersLoggedIn[e.Client] = null;

            e.Client.MessageReceived += OnMessageReceived;
        }

        /// <summary>
        ///     Player disconnected handler that updates the users online and logged in
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            if (_usersLoggedIn.ContainsKey(e.Client))
            {
                _usersLoggedIn.TryRemove(e.Client, out var username);

                if (username != null) _clients.TryRemove(username, out _);
            }
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
                if (message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Login + 1)) return;

                var client = e.Client;

                switch (message.Tag)
                {
                    case LoginTags.LoginUser:
                    {
                        LoginUser(client, message);
                        break;
                    }

                    case LoginTags.LogoutUser:
                    {
                        LogoutUser(client, 1);
                        break;
                    }

                    case LoginTags.AddUser:
                    {
                        AddUser(client, message);
                        break;
                    }
                }
            }
        }
        
        
        private PlayerData InitializePlayerData(string username)
        {
            // Extract game elements
            //
            byte nrResources = 3;
            string[] resourceNames = {"Silicon", "Lithium", "Titanium"};
            uint[] resourceInitialCount = { 1000, 450, 250 };
            
            byte nrRobots = 3;
            string[] robotNames = {"Worker", "Probe", "Crusher"};
            byte[] robotsInitialCount = {0, 0, 0};
            
            // Create player data
            string id = username;
            byte level = 1;
            ushort experience = 1;
            uint energy = 10000;

            // Create resources
            Resource[] resources = new Resource[nrResources];
            foreach (int iterator in Enumerable.Range(0, nrResources))
            {
                resources[iterator] = new Resource((byte)iterator, resourceNames[iterator], resourceInitialCount[iterator]);
            }

            // Create robots
            Robot[] robots = new Robot[nrRobots];
            foreach (int iterator in Enumerable.Range(0, nrRobots))
            {
                robots[iterator] = new Robot((byte)iterator, robotNames[iterator], level, robotsInitialCount[iterator]);
            }
            
            // Create tasks queue
            BuildTask[] conversionQueue = {};
            BuildTask[] upgradeQueue = {};
            BuildTask[] buildQueue = {};

            // Create player object
            return new PlayerData(id, level, experience, energy, resources, robots, conversionQueue, upgradeQueue, buildQueue);
        }

        #region ReceivedCalls

        /// <summary>
        ///     Checks the database to login the user
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The received message</param>
        private void LoginUser(IClient client, Message message)
        {
            // If users is already logged in
            if (_usersLoggedIn[client] != null)
            {
                using (var msg = Message.CreateEmpty(LoginTags.LoginSuccess))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }

                return;
            }

            var username = "";
            var password = "";

            using (var reader = message.GetReader())
            {
                try
                {
                    username = reader.ReadString();
                    password = Encryption.Decrypt(reader.ReadBytes(), _privateKey);
                }
                catch (Exception exception)
                {
                    // Return error 0 for Invalid Data Packages Received
                    InvalidData(client, LoginTags.LoginFailed, exception, "Failed to log in!");
                }
            }

            if (_clients.ContainsKey(username))
            {
                Logger.Info("Removing old client");
                // Removing old client
                var oldClient = _clients[username];
                LogoutUser(oldClient, 1);
                
                /*
                // Username is already in use, return Error 3
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 3);

                    using (var msg = Message.Create(LoginTags.LoginFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
                
                return;
                */
            }

            try
            {
                _database.DataLayer.GetUser(username, user =>
                {
                    if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Password))
                    {
                        _usersLoggedIn[client] = username;
                        _clients[username] = client;

                        using (var msg = Message.CreateEmpty(LoginTags.LoginSuccess))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }

                        if (_debug) Logger.Info("Successful login: " + client.ID + ").");
                    }
                    else
                    {
                        if (_debug) Logger.Info("User " + client.ID + " couldn't log in!");

                        // Return error 1 for wrong username/password combination
                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write((byte) 1);

                            using (var msg = Message.Create(LoginTags.LoginFailed, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Return error 2 for database error
                _database.DatabaseError(client, LoginTags.LoginFailed, ex);
            }
        }

        /// <summary>
        ///     Logs the user out
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="logoutType">The type of the logout (0 for normal and 1 for forced)</param>
        private void LogoutUser(IClient client, byte logoutType)
        {
            var username = _usersLoggedIn[client];
            _usersLoggedIn[client] = null;

            if (username != null) _clients.TryRemove(username, out _);
            
            if (_debug) Logger.Info("User " + username + " logged out!");

            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(logoutType);

                using (var msg = Message.Create(LoginTags.LogoutSuccess, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        /// <summary>
        ///     Checks the database to add a user and initialize it's data
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The received message</param>
        private void AddUser(IClient client, Message message)
        {
            if (!_allowAddUser) return;

            var username = "";
            var password = " ";

            using (var reader = message.GetReader())
            {
                try
                {
                    username = reader.ReadString();
                    password = BCrypt.Net.BCrypt.HashPassword(
                        Encryption.Decrypt(reader.ReadBytes(), _privateKey),
                        10);
                }
                catch (Exception ex)
                {
                    // Return error 0 for invalid data packages received
                    InvalidData(client, LoginTags.AddUserFailed, ex, "Failed to add user");
                }
            }

            try
            {
                _database.DataLayer.UsernameAvailable(username, isAvailable =>
                {
                    if (isAvailable)
                    {
                        _database.DataLayer.AddNewUser(username, password, () =>
                        {
                            if (_debug) Logger.Info("New user: " + username);

                            using (var msg = Message.CreateEmpty(LoginTags.AddUserSuccess))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                            
                            _database.DataLayer.AddPlayerData(InitializePlayerData(username), () =>
                            {
                                if(_debug) Logger.Info("Initialized data for user: " + username);
                            });
                        });
                    }
                    else
                    {
                        if (_debug) Logger.Info("User " + client.ID + " failed to sign up!");

                        // Return error 1 for wrong username/password combination
                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write((byte) 1);

                            using (var msg = Message.Create(LoginTags.AddUserFailed, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Return error 2 for database errors
                _database.DatabaseError(client, LoginTags.AddUserFailed, ex);
            }

        }

        #endregion
        
        #region Helpers

        /// <summary>
        /// Returns the player username
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public string GetPlayerUsername(IClient client)
        {
            return _usersLoggedIn[client];
        }

        #endregion

        #region Commands

        /// <summary>
        ///     Command for allowing new users to be added in the database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void AllowAddUserCommand(object sender, CommandEventArgs e)
        {
            switch (e.Arguments[0])
            {
                case "on":
                {
                    _allowAddUser = true;
                    Logger.Info("Adding users allowed");
                    break;
                }
                case "off":
                {
                    _allowAddUser = false;
                    Logger.Info("Adding users NOT allowed");
                    break;
                }
                default:
                {
                    Logger.Info("Enter [AllowAddUser on] or [AllowAddUser off]");
                    break;
                }
            }
        }

        /// <summary>
        ///     Command for adding a new user in the database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void AddUserCommand(object sender, CommandEventArgs e)
        {
            Logger.Info("Loading db");
            if (_database == null) _database = PluginManager.GetPluginByType<DatabaseProxy>();

            if (e.Arguments.Length != 2)
            {
                Logger.Warning("Invalid arguments. Enter [AddUser -username -password].");
                return;
            }

            var username = e.Arguments[0];
            var password = BCrypt.Net.BCrypt.HashPassword(e.Arguments[1], 10);

            Logger.Info("Loaded database");

            try
            {
                _database.DataLayer.UsernameAvailable(username, isAvailable =>
                {
                    if (isAvailable)
                    {
                        Logger.Info("Checking username");
                        _database.DataLayer.AddNewUser(username, password, () =>
                        {
                            if (_debug) Logger.Info("New user: " + username);
                        });
                    }
                    else
                    {
                        Logger.Warning("Username already in use");
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Database error: " + ex.Message + " - " + ex.StackTrace);
            }
        }

        /// <summary>
        ///     Command for deleting a user from the database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void DellUserCommand(object sender, CommandEventArgs e)
        {
            if (_database == null) _database = PluginManager.GetPluginByType<DatabaseProxy>();

            if (e.Arguments.Length != 2)
            {
                Logger.Warning("Invalid arguments. Enter [AddUser -username -password].");
                return;
            }

            var username = e.Arguments[0];

            try
            {
                _database.DataLayer.DeleteUser(username, () =>
                {
                    if (_debug) Logger.Info("Removed user: " + username);
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Database error: " + ex.Message + " - " + ex.StackTrace);
            }
        }

        /// <summary>
        ///     Command for showing the online users
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void OnlineUsersCommand(object sender, CommandEventArgs e)
        {
            Logger.Info(ClientManager.GetAllClients().Length + " users online");
        }

        /// <summary>
        ///     Command for showing the users logged in
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void LoggedInUsersCommand(object sender, CommandEventArgs e)
        {
            Logger.Info(_clients.Count + " users logged in");
        }

        /// <summary>
        ///     Command for setting the debug mode on/off
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void LpDebugCommand(object sender, CommandEventArgs e)
        {
            switch (e.Arguments[0])
            {
                case "on":
                {
                    _debug = true;
                    Logger.Info("Debug in active");
                    break;
                }
                case "off":
                {
                    _debug = false;
                    Logger.Info("Debug is not active");
                    break;
                }
                default:
                {
                    Logger.Info("Enter [LPDebug on] or [LPDebug off]");
                    break;
                }
            }
        }

        #endregion

        #region ErrorHandling

        /// <summary>
        ///     Send not logged in error to the user
        /// </summary>
        /// <param name="client">The client where the error occured</param>
        /// <param name="tag">The error tag</param>
        /// <param name="error">The error description</param>
        /// <returns>A bool with true for user logged in and false for user not logged in</returns>
        public bool PlayerLoggedIn(IClient client, ushort tag, string error)
        {
            if (_usersLoggedIn[client] != null) return true;

            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write((byte) 1);

                using (var msg = Message.Create(tag, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }

            Logger.Warning(error + " Player wasn't logged in.");
            return false;
        }

        /// <summary>
        ///     Sends an invalid data received to user
        /// </summary>
        /// <param name="client">The client where the error occured</param>
        /// <param name="tag">The error tag</param>
        /// <param name="e">The exception that occured</param>
        /// <param name="error">The error description</param>
        public void InvalidData(IClient client, ushort tag, Exception e, string error)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write((byte) 0);

                using (var msg = Message.Create(tag, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }

            Logger.Warning(error + " Invalid data received: " + e.Message + "-" + e.StackTrace);
        }

        #endregion
    }
}