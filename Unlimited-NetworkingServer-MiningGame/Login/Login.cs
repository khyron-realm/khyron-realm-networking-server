using System;
using System.Collections.Generic;
using System.IO;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Login
{
    public class Login : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        private Dictionary<string, IClient> _clients = new Dictionary<string, IClient>();
        private Dictionary<IClient, string> _usersLoggedIn = new Dictionary<IClient, string>();

        public override Command[] Commands => new Command[]
        {
            new Command("AllowAddUser", "Allow users to be added to the Database", "AllowAddUser [on/off]", AllowAddUser),
            new Command("AddUser", "Adds a User to the Database", "AddUser -username -password", AddUserCommand),
            new Command("DellUser", "Deletes a User from the Database", "DellUser -username", DellUserCommand),
            new Command("OnlineUsers", "Logs the number of online users", "OnlineUsers", OnlineUsersCommand),
            new Command("LoggedInUsers", "Logs the number of logged in users", "LoggedInUsers", OnlineUsersCommand),
            new Command("LPDebug", "Enables the debug logs for the Login Plugin", "LPDebug [on/off]", LpDebugCommand)
        };
        
        private const string PrivateKeyPath = @"Plugins/PrivateKey.xml";
        private static readonly object InitializeLock = new object();
        private bool _allowAddUser = true;
        private DatabaseProxy _database;
        private bool _debug = true;
        private string _privateKey;
        
        public Login(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            LoadRsaKey();

            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }

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

        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            if (_database == null)
            {
                lock (InitializeLock)
                {
                    if (_database == null)
                    {
                        _database = PluginManager.GetPluginByType<DatabaseProxy>();
                    }
                }
            }

            _usersLoggedIn[e.Client] = null;

            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            if (_usersLoggedIn.ContainsKey(e.Client))
            {
                var username = _usersLoggedIn[e.Client];
                _usersLoggedIn.Remove(e.Client);

                if (username != null)
                {
                    _clients.Remove(username);
                }
            }
        }

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
                        // If users is already logged in
                        if (_usersLoggedIn[client] != null)
                        {
                            using (var msg = Message.CreateEmpty(LoginTags.LoginSuccess))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }

                        string username = "";
                        string password = "";

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

                                    if (_debug)
                                    {
                                        Logger.Info("Successful login: " + client.ID + ").");
                                    }
                                }
                                else
                                {
                                    if (_debug)
                                    {
                                        Logger.Info("User " + client.ID + " couldn't log in!");
                                    }
                            
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

                        break;
                    }

                    case LoginTags.LogoutUser:
                    {
                        var username = _usersLoggedIn[client];
                        _usersLoggedIn[client] = null;

                        if (username != null)
                        {
                            _clients.Remove(username);
                        }

                        if (_debug)
                        {
                            Logger.Info("User " + client.ID + " logged out!");
                        }

                        using (var msg = Message.CreateEmpty(LoginTags.LoginSuccess))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                        
                        break;
                    }

                    case LoginTags.AddUser:
                    {
                        if (!_allowAddUser) return;

                        string username = "";
                        string password = " ";

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
                                        if (_debug)
                                        {
                                            Logger.Info("New user + " + username);
                                        }

                                        using (var msg = Message.CreateEmpty(LoginTags.AddUserSuccess))
                                        {
                                            client.SendMessage(msg, SendMode.Reliable);
                                        }
                                    });
                                }
                                else
                                {
                                    if (_debug)
                                    {
                                        Logger.Info("User " + client.ID + " failed to sign up!");
                                    }

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

                        break;
                    }
                }
            }
        }

        #region Commands

        private void AllowAddUser(object sender, CommandEventArgs e)
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
        
        private void AddUserCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
            }

            if (e.Arguments.Length != 2)
            {
                Logger.Warning("Invalid arguments. Enter [AddUser -username -password].");
                return;
            }

            var username = e.Arguments[0];
            var password = BCrypt.Net.BCrypt.HashPassword(e.Arguments[1], 10);

            try
            {
                _database.DataLayer.UsernameAvailable(username, isAvailable =>
                {
                    if (isAvailable)
                    {
                        _database.DataLayer.AddNewUser(username, password, () =>
                        {
                            if (_debug)
                            {
                                Logger.Info("New user " + username);
                            }
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

        private void DellUserCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
            }
            
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
                    if (_debug)
                    {
                        Logger.Info("Removed user: " + username);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Database error: " + ex.Message + " - " + ex.StackTrace);
            }
        }

        private void OnlineUsersCommand(object sender, CommandEventArgs e)
        {
            Logger.Info(ClientManager.GetAllClients().Length + " users online in");
        }

        private void LoggedInUsersCommand(object sender, CommandEventArgs e)
        {
            Logger.Info(_clients.Count + " users logged in");
        }

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

        public bool PlayerLoggedIn(IClient client, ushort tag, string error)
        {
            if (_usersLoggedIn[client] != null)
            {
                return true;
            }

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