using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Login
{
    public class Login : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        private Dictionary<string, IClient> _clients = new Dictionary<string, IClient>();
        private Dictionary<IClient, string> _playersLoggedIn = new Dictionary<IClient, string>();

        public override Command[] Commands => new Command[]
        {
            new Command("AddUser", "Adds a User to the Database", "AddUser -username -password", AddUserCommand)
        };
        
        private const string PrivateKeyPath = @"Plugins/PrivateKey.xml";
        private bool _allowAddPlayer = true;
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
            //
            // To Be Made - DatabaseProxy plugin verification

            _playersLoggedIn[e.Client] = null;

            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            if (_playersLoggedIn.ContainsKey(e.Client))
            {
                var username = _playersLoggedIn[e.Client];
                _playersLoggedIn.Remove(e.Client);

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
                if (message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Login + 1))
                {
                    return;
                }
                
                var client = e.Client;

                switch (message.Tag)
                {
                    case LoginTags.LoginPlayer:
                    {
                        // If users is already logged in
                        if (_playersLoggedIn[client] != null)
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
                            //
                            // To Be Made - Database user verification
                            
                            string dbUsername = "";
                            string dbPassword = "";
                            
                            if (username == dbUsername && password == dbPassword)
                            {
                                _playersLoggedIn[client] = username;
                                _clients[username] = client;

                                using (var msg = Message.CreateEmpty(LoginTags.LoginSuccess))
                                {
                                    client.SendMessage(msg, SendMode.Reliable);
                                }
                                    
                                Logger.Info("Successful login: " + client.ID + ").");
                            }
                            else
                            {
                                Logger.Info("Player " + client.ID + " couldn't log in!");
                                
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
                        }
                        catch (Exception ex)
                        {
                            // Return error 2 for database error
                        }

                        break;
                    }

                    case LoginTags.LogoutPlayer:
                    {
                        var username = _playersLoggedIn[client];
                        _playersLoggedIn[client] = null;

                        if (username != null)
                        {
                            _clients.Remove(username);
                        }

                        Logger.Info("Player " + client.ID + " logged out!");

                        using (var msg = Message.CreateEmpty(LoginTags.LoginSuccess))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                        
                        break;
                    }

                    case LoginTags.AddPlayer:
                    {
                        if (!_allowAddPlayer) return;

                        string username = "";
                        string password = " ";

                        try
                        {
                            using (var reader = message.GetReader())
                            {
                                username = reader.ReadString();
                                password = BCrypt.Net.BCrypt.HashPassword(
                                    Encryption.Decrypt(reader.ReadBytes(), _privateKey), 10);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Return error 0 for invalid data packages received
                            InvalidData(client, LoginTags.AddPlayedFailed, ex, "Failed to add player");
                        }

                        try
                        {
                            //
                            // To Be Made - Database user add
                            
                            Logger.Info("Add user + " + username);
                        }
                        catch (Exception ex)
                        {
                            // Return error 2 for database errors
                        }

                        break;
                    }
                }
            }
        }

        #region Commands

        private void AddUserCommand(object sender, CommandEventArgs e)
        {
            //
            // Check database connection

            if (e.Arguments.Length != 2)
            {
                Logger.Warning("Invalid arguments. Enter [AddUser -username -password].");
                return;
            }

            string username = e.Arguments[0];
            string password = BCrypt.Net.BCrypt.HashPassword(e.Arguments[1], 10);

            try
            {
                //
                // Database user add
                
                Logger.Info("New user: " + username);
            }
            catch (Exception ex)
            {
                Logger.Error("Database error: " + ex.Message + " - " + ex.StackTrace);
            }
        }

        #endregion

        #region ErrorHandling

        public bool PlayerLoggedIn(IClient client, ushort tag, string error)
        {
            if (_playersLoggedIn[client] != null)
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