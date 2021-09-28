using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame
{
    public class Login : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        private Dictionary<string, IClient> _clients = new Dictionary<string, IClient>();
        private Dictionary<IClient, string> _playersLoggedIn = new Dictionary<IClient, string>();
        
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
                var client = e.Client;

                switch (message.Tag)
                {
                    case Tags.LoginPlayer:
                    {
                        // If users is already logged in
                        if (_playersLoggedIn[client] != null)
                        {
                            using (var msg = Message.CreateEmpty(Tags.LoginSuccess))
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
                                InvalidData(client, Tags.LoginFailed, exception, "Failed to log in!");
                            }
                        }

                        if (_clients.ContainsKey(username))
                        {
                            // Username is already in use, return Error 3
                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write((byte) 3);

                                using (var msg = Message.Create(Tags.LoginFailed, writer))
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

                                using (var msg = Message.CreateEmpty(Tags.LoginSuccess))
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

                                    using (var msg = Message.Create(Tags.LoginFailed, writer))
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

                    case Tags.LogoutPlayer:
                    {
                        var username = _playersLoggedIn[client];
                        _playersLoggedIn[client] = null;

                        if (username != null)
                        {
                            _clients.Remove(username);
                        }

                        Logger.Info("Player " + client.ID + " logged out!");

                        using (var msg = Message.CreateEmpty(Tags.LoginSuccess))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                        
                        break;
                    }

                    case Tags.AddPlayer:
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
                            InvalidData(client, Tags.AddPlayedFailed, ex, "Failed to add player");
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
    }
}