using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Auctions;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Chat
{
    /// <summary>
    ///     Chat manager that handles the chat messages
    /// </summary>
    public class ChatPlugin: Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        private static readonly object InitializeLock = new object();
        private bool _debug = true;
        private LoginPlugin _loginPlugin;
        private AuctionsPlugin _auctionsPlugin;

        public ConcurrentDictionary<string, ChatGroup> ChatGroups { get; } =
            new ConcurrentDictionary<string, ChatGroup>();

        public ConcurrentDictionary<string, List<ChatGroup>> ChatGroupsOfPlayer { get; } =
            new ConcurrentDictionary<string, List<ChatGroup>>();

        public override Command[] Commands => new[]
        {
            new Command("Groups", "Show all chat groups", "groups [username]", GetChatGroupsCommand)
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
                        _loginPlugin.onLogout += RemovePlayerFromChatGroup;
                        ChatGroups["General"] = new ChatGroup("General");
                    }
                }
            }

            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag < Tags.Tags.TagsPerPlugin * Tags.Tags.Chat ||
                    message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Chat + 1)) return;

                // Get client
                var client = e.Client;

                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, ChatTags.RequestFailed, "Player not logged in.")) 
                    return;

                switch (message.Tag)
                {
                    case ChatTags.PrivateMessage:
                    {
                        PrivateMessage(client, message);
                        break;
                    }

                    case ChatTags.RoomMessage:
                    {
                        RoomMessage(client, message);
                        break;
                    }

                    case ChatTags.GroupMessage:
                    {
                        GroupMessage(client, message);
                        break;
                    }

                    case ChatTags.JoinGroup:
                    {
                        JoinGroup(client, message);
                        break;
                    }

                    case ChatTags.LeaveGroup:
                    {
                        LeaveGroup(client, message);
                        break;
                    }

                    case ChatTags.GetActiveGroups:
                    {
                        GetActiveGroups(client, message);
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Sends the private message to the receiver
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private void PrivateMessage(IClient client, Message message)
        {
            var senderName = _loginPlugin.GetPlayerUsername(client);
            string receiver = "";
            string content = "";

            try
            {
                using (var reader = message.GetReader())
                {
                    receiver = reader.ReadString();
                    content = reader.ReadString();
                }
            }
            catch (Exception exception)
            {
                InvalidData(client, ChatTags.RequestFailed, exception, "Invalid data packages received");
                return;
            }

            if (!_loginPlugin.ClientExistent(receiver))
            {
                // If the receiver isn't logged in -> return error 3
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 3);

                    using (var msg = Message.Create(ChatTags.MessageFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }

                if (_debug)
                {
                    Logger.Info("Send message failed. Receiver: " + receiver + " wasn't logged in");
                }
            }

            var receivingClient = _loginPlugin.GetClient(receiver);
            
            // Let the sender know message got transmitted
            using (var msg = Message.CreateEmpty(ChatTags.SuccessfulPrivateMessage))
            {
                client.SendMessage(msg, SendMode.Reliable);
            }
            
            // Let receiver know about the message
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(senderName);
                writer.Write(content);

                using (var msg = Message.Create(ChatTags.PrivateMessage, writer))
                {
                    receivingClient.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        /// <summary>
        ///     Sends a message to the room players
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private void RoomMessage(IClient client, Message message)
        {
            var senderName = _loginPlugin.GetPlayerUsername(client);
            ushort roomId = 0;
            string content = "";
            
            try
            {
                using (var reader = message.GetReader())
                {
                    roomId = reader.ReadUInt16();
                    content = reader.ReadString();
                }
            }
            catch (Exception exception)
            {
                InvalidData(client, ChatTags.RequestFailed, exception, "Invalid data packages received");
                return;
            }

            if (!_auctionsPlugin.AuctionRoomList[roomId].Clients.Contains(client))
            {
                // If the player is not in the room -> return error 2
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 2);

                    using (var msg = Message.Create(ChatTags.MessageFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }

                if (_debug)
                {
                    Logger.Info("Send message failed. Player: " + _loginPlugin.GetPlayerUsername(client) + " wasn't part of the room");
                }

                return;
            }
            
            // Let receiver know about the message
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(senderName);
                writer.Write(content);

                using (var msg = Message.Create(ChatTags.RoomMessage, writer))
                {
                    foreach (var cl in _auctionsPlugin.AuctionRoomList[roomId].Clients)
                    {
                        cl.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
        }
        
        /// <summary>
        ///     Send a message to the group players
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private void GroupMessage(IClient client, Message message)
        {
            var senderName = _loginPlugin.GetPlayerUsername(client);
            string groupName = "";
            string content = "";
            
            try
            {
                using (var reader = message.GetReader())
                {
                    groupName = reader.ReadString();
                    content = reader.ReadString();
                }
            }
            catch (Exception exception)
            {
                InvalidData(client, ChatTags.MessageFailed, exception, "Invalid data packages received");
                return;
            }

            if (!ChatGroups[groupName].Users.Values.Contains(client))
            {
                // If the player is not in the chat group -> return error 2
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 2);

                    using (var msg = Message.Create(ChatTags.MessageFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }

                if (_debug)
                {
                    Logger.Info("Send message failed. Player: " + _loginPlugin.GetPlayerUsername(client) + " wasn't part of the chat group");
                }

                return;
            }
            
            // Let receiver know about the message
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(groupName);
                writer.Write(senderName);
                writer.Write(content);

                using (var msg = Message.Create(ChatTags.RoomMessage, writer))
                {
                    foreach (var cl in ChatGroups[groupName].Users.Values)
                    {
                        cl.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
        }
        
        /// <summary>
        ///     Joins a group of players
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private void JoinGroup(IClient client, Message message)
        {
            var playerName = _loginPlugin.GetPlayerUsername(client);
            string groupName = "";

            try
            {
                using (var reader = message.GetReader())
                {
                    groupName = reader.ReadString();
                }
            }
            catch (Exception exception)
            {
                InvalidData(client, ChatTags.RequestFailed, exception, "Invalid data packages received");
                return;
            }

            var chatGroup = ChatGroups
                .FirstOrDefault(x => string.Equals(x.Key, groupName, StringComparison.CurrentCultureIgnoreCase)).Value;
            if (chatGroup == null)
            {
                chatGroup = new ChatGroup(groupName);
                ChatGroups[groupName] = chatGroup;
            }

            if (!chatGroup.AddPlayer(playerName, client))
            {
                // If the player is already in the chat group -> return error 2
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 2);

                    using (var msg = Message.Create(ChatTags.JoinGroupFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
                return;
            }

            if (!ChatGroupsOfPlayer.ContainsKey(playerName))
            {
                ChatGroupsOfPlayer[playerName] = new List<ChatGroup>();
            }
            ChatGroupsOfPlayer[playerName].Add(chatGroup);
            
            // Send confirmation to sender
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(chatGroup);

                using (var msg = Message.Create(ChatTags.JoinGroup, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }

            if (_debug)
            {
                Logger.Info("Player joined ChatGroup: " + groupName);
            }
        }
        
        /// <summary>
        ///     Leave a group of players
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private void LeaveGroup(IClient client, Message message)
        {
            var playerName = _loginPlugin.GetPlayerUsername(client);
            string groupName = "";

            try
            {
                using (var reader = message.GetReader())
                {
                    groupName = reader.ReadString();
                }
            }
            catch (Exception exception)
            {
                InvalidData(client, ChatTags.JoinGroupFailed, exception, "Invalid data packages received");
                return;
            }

            var chatGroup = ChatGroups
                .FirstOrDefault(x => string.Equals(x.Key, groupName, StringComparison.CurrentCultureIgnoreCase)).Value;
            if (chatGroup == null)
            {
                // No such Chat Group -> return error 2
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 2);

                    using (var msg = Message.Create(ChatTags.LeaveGroupFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
                return;
            }
            chatGroup.RemovePlayer(playerName);
            
            // Remove chat group if the player was the last in it
            if (chatGroup.Users.Count == 0 && chatGroup.Name != "General")
            {
                ChatGroups.TryRemove(chatGroup.Name, out _);
            }
            
            // Remove chat group from the players groups
            if (ChatGroupsOfPlayer[playerName].Count == 0)
            {
                ChatGroupsOfPlayer.TryRemove(playerName, out _);
            }
            else
            {
                ChatGroupsOfPlayer[playerName].Remove(chatGroup);
            }
            
            // Send confirmation to sender
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(chatGroup.Name);

                using (var msg = Message.Create(ChatTags.LeaveGroup, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }

            if (_debug)
            {
                Logger.Info("Player left ChatGroup: " + groupName);
            }
        }
        
        /// <summary>
        ///     Get the list of active groups
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        private void GetActiveGroups(IClient client, Message message)
        {
            var groupNames = ChatGroups.Values.Select(chatGroup => chatGroup.Name).ToArray();
            
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(groupNames);

                using (var msg = Message.Create(ChatTags.JoinGroup, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }
        
        /// <summary>
        ///     Removes a player from a chat group
        /// </summary>
        /// <param name="username"></param>
        private void RemovePlayerFromChatGroup(string username)
        {
            if(!ChatGroupsOfPlayer.ContainsKey(username)) return;

            foreach (var chatGroup in ChatGroupsOfPlayer[username])
            {
                ChatGroups[chatGroup.Name].RemovePlayer(username);
                if (chatGroup.Users.Count == 0 && chatGroup.Name != "General")
                {
                    ChatGroups.TryRemove(chatGroup.Name, out _);
                }
            }

            ChatGroupsOfPlayer.TryRemove(username, out _);
        }

        #region Commands
        
        /// <summary>
        ///     Command for showing the chat groups
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetChatGroupsCommand(object sender, CommandEventArgs e)
        {
            Logger.Info("Active Chat Groups:");

            var chatGroups = ChatGroups.Values.ToList();
            
            if (e.Arguments.Length == 0)
            {
                foreach (var chatGroup in chatGroups)
                {
                    Logger.Info(chatGroup.Name + " (" + chatGroup.Users.Count + ")");
                }
            }
            else
            {
                var username = e.Arguments[0];

                if (!ChatGroupsOfPlayer.ContainsKey(username))
                {
                    Logger.Info(username + " doesn't exist in any chat groups");
                    return;
                }

                foreach (var chatGroup in ChatGroupsOfPlayer[username])
                {
                    Logger.Info(chatGroup.Name);
                }
            }
        }

        #endregion
        
        #region ErrorHandling

        /// <summary>
        ///     Sends an invalid data received to user
        /// </summary>
        /// <param name="client">The client where the error occured</param>
        /// <param name="tag">The error tag</param>
        /// <param name="e">The exception that occured</param>
        /// <param name="error">The error description</param>
        private void InvalidData(IClient client, ushort tag, Exception e, string error)
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