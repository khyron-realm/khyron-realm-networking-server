using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Friends
{
    /// <summary>
    ///     Friends manager that handles friends and requests
    /// </summary>
    public class FriendsPlugin : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        public override Command[] Commands => new[]
        {
            new Command("AddFriend", "Adds a User to the Database", "AddFriend [friend_name]", AddFriendCommand),
            new Command("DelFriend", "Deletes a User from the Database", "DelFriend [friend_name]", DelFriendCommand)
        };

        private const string ConfigPath = @"Plugins/FriendsPlugin.xml";
        private DatabaseProxy _database;
        private LoginPlugin _loginPlugin;
        private bool _debug = true;

        protected override void Loaded(LoadedEventArgs args)
        {
            if (_database == null) _database = PluginManager.GetPluginByType<DatabaseProxy>();
            if (_loginPlugin == null) _loginPlugin = PluginManager.GetPluginByType<LoginPlugin>();
            
            _loginPlugin.OnLogout += LogoutFriend;
        }
        
        public FriendsPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            LoadConfig();
            ClientManager.ClientConnected += OnPlayerConnected;
        }
        
        /// <summary>
        ///     Player connected handler that initializes the database and sends connection confirmation to client
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            e.Client.MessageReceived += OnMessageReceived;
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
                if (message.Tag < Tags.Tags.TagsPerPlugin * Tags.Tags.Friends ||
                    message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Friends + 1)) return;

                // Get client
                var client = e.Client;

                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, FriendsTags.RequestFailed, "Player not logged in.")) 
                    return;

                switch (message.Tag)
                {
                    case FriendsTags.FriendRequest:
                    {
                        FriendRequest(client, message);
                        break;
                    }

                    case FriendsTags.DeclineRequest:
                    {
                        DeclineRequest(client, message);
                        break;
                    }

                    case FriendsTags.AcceptRequest:
                    {
                        AcceptRequest(client, message);
                        break;
                    }
                    
                    case FriendsTags.RemoveFriend:
                    {
                        RemoveFriend(client, message);
                        break;
                    }

                    case FriendsTags.GetAllFriends:
                    {
                        GetAllFriends(client, message);
                        break;
                    }
                }
            }
        }

        #region ReceivedCalls

        /// <summary>
        ///     
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void FriendRequest(IClient client, Message message)
        {
            var senderName = _loginPlugin.GetPlayerUsername(client);
            string receiver;

            try
            {
                using (var reader = message.GetReader())
                {
                    receiver = reader.ReadString();
                }
            }
            catch (Exception ex)
            {
                // Return Error 0 for Invalid Data Packages Received
                _loginPlugin.InvalidData(client, FriendsTags.RequestFailed, ex, "Friend Request Failed! ");
                return;
            }

            try
            {
                _database.DataLayer.GetFriends(receiver, receiverUser =>
                {
                    if (receiverUser == null)
                    {
                        // No user with that name found -> return error 3
                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write((byte) 3);

                            using (var msg = Message.Create(FriendsTags.RequestFailed, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }

                        if (_debug)
                        {
                            Logger.Info("No user named " + receiver + " found!");
                        }
                        return;
                    }

                    if (receiverUser.Friends.Contains(senderName) ||
                        receiverUser.OpenFriendRequests.Contains(senderName))
                    {
                        // Users are already friends or have an open request -> return error 4
                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write((byte) 4);

                            using (var msg = Message.Create(FriendsTags.RequestFailed, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }

                        if (_debug)
                        {
                            Logger.Info("Request failed, " + senderName + " and " + receiver +
                                       " were already friends or had an open friend request!");
                        }
                        return;
                    }

                    // Save the request in the database to both users
                    _database.DataLayer.AddRequest(senderName, receiver, () =>
                    {
                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(receiver);

                            using (var msg = Message.Create(FriendsTags.RequestSuccess, writer))
                            {
                                client.SendMessage(msg, SendMode.Reliable);
                            }
                        }

                        if (_debug)
                        {
                            Logger.Info(senderName + " wants to add " + receiver + " as a friend!");
                        }

                        // If Receiver is currently logged in, let him know right away
                        if (_loginPlugin.ClientExistent(receiver))
                        {
                            var receivingClient = _loginPlugin.GetClient(receiver);

                            using (var writer = DarkRiftWriter.Create())
                            {
                                writer.Write(senderName);

                                using (var msg = Message.Create(FriendsTags.FriendRequest, writer))
                                {
                                    receivingClient.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                // Return Error 2 for Database error
                _database.DatabaseError(client, FriendsTags.RequestFailed, ex);
            }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void DeclineRequest(IClient client, Message message)
        {
            var senderName = _loginPlugin.GetPlayerUsername(client);
            string receiver;

            try
            {
                using (var reader = message.GetReader())
                {
                    receiver = reader.ReadString();
                }
            }
            catch (Exception ex)
            {
                // Return Error 0 for Invalid Data Packages Received
                _loginPlugin.InvalidData(client, FriendsTags.DeclineRequestFailed, ex, "Decline Request Failed!");
                return;
            }

            try
            {
                // Delete the request from the database for both users
                _database.DataLayer.RemoveRequest(senderName, receiver, () =>
                {
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write(receiver);
                        writer.Write(true);

                        using (var msg = Message.Create(FriendsTags.DeclineRequestSuccess, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }

                    if (_debug)
                    {
                        Logger.Info(senderName + " declined " + receiver + "'s friend request.");
                    }

                    // If Receiver is currently logged in, let him know right away
                    if (_loginPlugin.ClientExistent(receiver))
                    {
                        var receivingClient = _loginPlugin.GetClient(receiver);

                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(senderName);
                            writer.Write(false);

                            using (var msg = Message.Create(FriendsTags.DeclineRequestSuccess, writer))
                            {
                                receivingClient.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Return Error 2 for Database error
                _database.DatabaseError(client, FriendsTags.DeclineRequestFailed, ex);
            }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void AcceptRequest(IClient client, Message message)
        { 
            var senderName = _loginPlugin.GetPlayerUsername(client);
            string receiver;

            try
            {
                using (var reader = message.GetReader())
                {
                    receiver = reader.ReadString();
                }
            }
            catch (Exception ex)
            {
                // Return Error 0 for Invalid Data Packages Received
                _loginPlugin.InvalidData(client, FriendsTags.AcceptRequestFailed, ex, "Accept Request Failed!");
                return;
            }

            try
            {
                // Delete the request from the database for both users and add their names to their friend list
                _database.DataLayer.AddFriend(senderName, receiver, () =>
                {
                    var receiverOnline = _loginPlugin.ClientExistent(receiver);

                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write(receiver);
                        writer.Write(receiverOnline);

                        using (var msg = Message.Create(FriendsTags.AcceptRequestSuccess, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }

                    if (_debug)
                    {
                        Logger.Info(senderName + " accepted " + receiver + "'s friend request.");
                    }

                    // If Receiver is currently logged in, let him know right away
                    if (receiverOnline)
                    {
                        var receivingClient = _loginPlugin.GetClient(receiver);

                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(senderName);
                            writer.Write(true);

                            using (var msg = Message.Create(FriendsTags.AcceptRequestSuccess, writer))
                            {
                                receivingClient.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Return Error 2 for Database error
                _database.DatabaseError(client, FriendsTags.AcceptRequestFailed, ex);
            }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void RemoveFriend(IClient client, Message message)
        {
            var senderName = _loginPlugin.GetPlayerUsername(client);
            string receiver;

            try
            {
                using (var reader = message.GetReader())
                {
                    receiver = reader.ReadString();
                }
            }
            catch (Exception ex)
            {
                // Return Error 0 for Invalid Data Packages Received
                _loginPlugin.InvalidData(client, FriendsTags.RemoveFriendFailed, ex, "Remove Friend Failed!");
                return;
            }

            try
            {
                // Delete the names from the friend list in the database for both users
                _database.DataLayer.RemoveFriend(senderName, receiver, () =>
                {
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write(receiver);
                        writer.Write(true);

                        using (var msg = Message.Create(FriendsTags.RemoveFriendSuccess, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }

                    if (_debug)
                    {
                        Logger.Info(senderName + " removed " + receiver + " as a friend.");
                    }

                    // If Receiver is currently logged in, let him know right away
                    if (_loginPlugin.ClientExistent(receiver))
                    {
                        var receivingClient = _loginPlugin.GetClient(receiver);

                        using (var writer = DarkRiftWriter.Create())
                        {
                            writer.Write(senderName);
                            writer.Write(false);

                            using (var msg = Message.Create(FriendsTags.RemoveFriendSuccess, writer))
                            {
                                receivingClient.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                // Return Error 2 for Database error
                _database.DatabaseError(client, FriendsTags.RemoveFriendFailed, ex);
            }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void GetAllFriends(IClient client, Message message)
        {
            var senderName = _loginPlugin.GetPlayerUsername(client);

            try
            {
                _database.DataLayer.GetFriends(senderName, friendList =>
                {
                    var onlineFriends = new List<string>();
                    var offlineFriends = new List<string>();

                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write(senderName);

                        foreach (var friend in friendList.Friends)
                        {
                            if (_loginPlugin.ClientExistent(friend))
                            {
                                onlineFriends.Add(friend);

                                // let online friends know he logged in
                                var cl = _loginPlugin.GetClient(friend);

                                using (var msg = Message.Create(FriendsTags.FriendLoggedIn, writer))
                                {
                                    cl.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                            else
                            {
                                offlineFriends.Add(friend);
                            }
                        }
                    }

                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write(onlineFriends.ToArray());
                        writer.Write(offlineFriends.ToArray());
                        writer.Write(friendList.OpenFriendRequests.ToArray());
                        writer.Write(friendList.UnansweredFriendRequests.ToArray());

                        using (var msg = Message.Create(FriendsTags.GetAllFriends, writer))
                        {
                            client.SendMessage(msg, SendMode.Reliable);
                        }
                    }

                    if (_debug)
                    {
                        Logger.Info("Got friends for " + senderName);
                    }
                });
            }
            catch (Exception ex)
            {
                // Return Error 2 for Database error
                _database.DatabaseError(client, FriendsTags.GetAllFriendsFailed, ex);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     
        /// </summary>
        /// <param name="username">The player username</param>
        public void LogoutFriend(IClient client, string username)
        {
            try
            {
                _database.DataLayer.GetFriends(username, friendList =>
                {
                    var friends = friendList.Friends;
                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write(username);

                        foreach (var friend in friends)
                        {
                            if (_loginPlugin.ClientExistent(friend))
                            {
                                // let online friends know he logged out
                                var clientFriend = _loginPlugin.GetClient(friend);

                                using (var msg = Message.Create(FriendsTags.FriendLoggedOut, writer))
                                {
                                    clientFriend.SendMessage(msg, SendMode.Reliable);
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error($"Database Error. Failed to notify friends of Logout! \n\n{ex.Message}\n{ex.StackTrace}");
            }
        }
        
        private void LoadConfig()
        {
            XDocument document;

            if (!File.Exists(ConfigPath))
            {
                document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("Settings for the Friends Plugin"),
                    new XElement("Variables", new XAttribute("Debug", true))
                );
                try
                {
                    document.Save(ConfigPath);
                    Logger.Info("Created /Plugins/FriendsPlugin.xml!");
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to create FriendsPlugin.xml: " + ex.Message + " - " + ex.StackTrace);
                }
            }
            else
            {
                try
                {
                    document = XDocument.Load(ConfigPath);
                    _debug = document.Element("Variables").Attribute("Debug").Value == "true";
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to load FriendsPlugin.xml: " + ex.Message + " - " + ex.StackTrace);
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFriendCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
            }

            if (e.Arguments.Length != 2)
            {
                Logger.Warning("Invalid arguments. Enter [AddFríend name friend].");
                return;
            }

            var username = e.Arguments[0];
            var friend = e.Arguments[1];

            try
            {
                _database.DataLayer.AddFriend(username, friend, () =>
                {
                    if (_debug)
                    {
                        Logger.Info("Added " + friend + " as a friend of " + username);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Database Error: " + ex.Message + " - " + ex.StackTrace);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DelFriendCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
            }

            if (e.Arguments.Length != 2)
            {
                Logger.Warning("Invalid arguments. Enter [AddFriend name friend].");
                return;
            }

            var username = e.Arguments[0];
            var friend = e.Arguments[1];

            try
            {
                _database.DataLayer.RemoveFriend(username, friend, () =>
                {
                    if (_debug)
                    {
                        Logger.Info("Removed " + friend + " as a friend of " + username);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Database Error: " + ex.Message + " - " + ex.StackTrace);
            }
        }

        #endregion
    }
}