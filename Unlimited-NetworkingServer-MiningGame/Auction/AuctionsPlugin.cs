using System;
using System.Collections.Concurrent;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Mine;
using Unlimited_NetworkingServer_MiningGame.Tags;

namespace Unlimited_NetworkingServer_MiningGame.Auction
{
    /// <summary>
    ///     Auctions manager that handles the auction and rooms messages
    /// </summary>
    public class AuctionsPlugin : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        public override Command[] Commands => new[]
        {
            new Command("AuctionRooms", "Show all auction rooms", "", GetRoomsCommand)
        };

        public ConcurrentDictionary<ushort, AuctionRoom> AuctionRoomList { get; } =
            new ConcurrentDictionary<ushort, AuctionRoom>();

        private static readonly object InitializeLock = new object();

        private readonly ConcurrentDictionary<ushort, AuctionRoom> _playersInRooms =
            new ConcurrentDictionary<ushort, AuctionRoom>();
        private bool _debug = true;
        private LoginPlugin _loginPlugin;

        public AuctionsPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
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
                    }
                }
            }

            e.Client.MessageReceived += OnMessageReceived;
        }
        
        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
        {
            LeaveAuctionRoom(e.Client);
        }
        
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (var message = e.GetMessage())
            {
                // Check if message is meant for this plugin
                if (message.Tag < Tags.Tags.TagsPerPlugin * Tags.Tags.Auctions ||
                    message.Tag >= Tags.Tags.TagsPerPlugin * (Tags.Tags.Auctions + 1)) return;

                var client = e.Client;

                // If player isn't logged in, return error 1
                if (!_loginPlugin.PlayerLoggedIn(client, Tags.AuctionTags.CreateFailed, "Player not logged in."))
                    return;

                switch (message.Tag)
                {
                    case AuctionTags.Create:
                    {
                        CreateAuctionRoom(client, message);
                        break;
                    }

                    case AuctionTags.Join:
                    {
                        JoinAuctionRoom(client, message);
                        break;
                    }

                    case AuctionTags.Leave:
                    {
                        LeaveAuctionRoom(client);
                        break;
                    }

                    case AuctionTags.GetOpenRooms:
                    {
                        GetOpenRooms(client);
                        break;
                    }

                    case AuctionTags.StartAuction:
                    {
                        StartAuction(client, message);
                        break;
                    }

                    case AuctionTags.AddBid:
                    {
                        AddBid(client, message);
                        break;
                    }
                }
            }
        }

        #region ReceivedCalls

        /// <summary>
        ///     Create a new user generated auction room
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void CreateAuctionRoom(IClient client, Message message)
        {
            string roomName;
            bool isVisible;

            try
            {
                using (var reader = message.GetReader())
                {
                    roomName = reader.ReadString();
                    isVisible = reader.ReadBoolean();
                }
            }
            catch (Exception ex)
            {
                // Return Error 0 for Invalid Data Packages Received
                _loginPlugin.InvalidData(client, AuctionTags.CreateFailed, ex, "Auction room create failed");
                return;
            }

            roomName = AdjustAuctionRoomName(roomName, _loginPlugin.GetPlayerUsername(client));
            var roomId = GenerateAuctionRoomId();
            var seed = new MineSeed(0, 0, 0, 0);            // TO-DO
            var mine = new MineData(0, 9000, seed);                           // TO-DO
            var room = new AuctionRoom(roomId, roomName, isVisible, mine);          
            var player = new Player(client.ID, _loginPlugin.GetPlayerUsername(client), true);

            room.AddPlayer(player, client);
            AuctionRoomList.TryAdd(roomId, room);
            _playersInRooms.TryAdd(client.ID, room);

            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(room);
                writer.Write(player);

                using (var msg = Message.Create(AuctionTags.CreateSuccess, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }

            if (_debug)
            {
                Logger.Info("Creating auction room " + roomId + ": " + room.Name);
            }
        }
        
        /// <summary>
        ///     Join an available auction room
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void JoinAuctionRoom(IClient client, Message message)
        {
            ushort roomId = 0;

            try
            {
                using (var reader = message.GetReader())
                {
                    roomId = reader.ReadUInt16();
                }
            }
            catch (Exception ex)
            {
                // Return error 0 for invalid data packages received
                _loginPlugin.InvalidData(client, AuctionTags.JoinFailed, ex, "Auction room join failed");
            }

            if (!AuctionRoomList.ContainsKey(roomId))
            {
                // Return error 3 for not existent room
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 3);

                    using (var msg = Message.Create(AuctionTags.JoinFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }

                if (_debug)
                {
                    Logger.Info("Room join failed! Room " + roomId + " doesn't exist anymore");
                }

                return;
            }

            var room = AuctionRoomList[roomId];
            var newPlayer = new Player(client.ID, _loginPlugin.GetPlayerUsername(client), false);

            // Check if player already is in an active room -> send error 2
            if(_playersInRooms.ContainsKey(client.ID))
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 2);

                    using (var msg = Message.Create(AuctionTags.JoinFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }

                if (_debug)
                {
                    Logger.Info("User " + _loginPlugin.GetPlayerUsername(client) + " couldn't join Room " + room.Id +
                                ", since he already is in Room: " +
                                _playersInRooms[client.ID]);
                }
            }

            // Try to join room
            if (room.AddPlayer(newPlayer, client))
            {
                _playersInRooms[client.ID] = room;

                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(room);

                    foreach (var player in room.PlayerList)
                    {
                        writer.Write(player);
                    }

                    using (var msg = Message.Create(AuctionTags.JoinSuccess, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
                
                // Let the other clients know
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(newPlayer);

                    using (var msg = Message.Create(AuctionTags.PlayerJoined, writer))
                    {
                        foreach (var cl in room.Clients.Where(c => c.ID != client.ID))
                        {
                            cl.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }

                if (_debug)
                {
                    Logger.Info("User " + client.ID + " joined room " + room.Id);
                }
            }
            // Room full or has started -> send error 2
            else
            {
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 2);

                    using (var msg = Message.Create(AuctionTags.JoinFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }

                if (_debug)
                {
                    Logger.Info("User " + client.ID + " couldn't join, since Room " + room.Id + " was either full or had started");
                }
            }
        }
        
        /// <summary>
        ///     Leave an auction room
        /// </summary>
        /// <param name="client">The connected client</param>
        private void LeaveAuctionRoom(IClient client)
        {
            var id = client.ID;
            if(!_playersInRooms.ContainsKey(id)) return;

            var room = _playersInRooms[id];
            var leaverName = room.PlayerList.FirstOrDefault(p => p.Id == client.ID)?.Name;
            _playersInRooms.TryRemove(id, out _);

            if (room.RemovePlayer(client))
            {
                // Only message user if still connected
                // (would cause error if LeaveRoom is called from disconnect otherwise)
                if (client.ConnectionState == ConnectionState.Connected)
                {
                    using (var msg = Message.CreateEmpty(AuctionTags.LeaveSuccess))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
                
                // Remove room if it's empty
                if (room.PlayerList.Count == 0)
                {
                    AuctionRoomList.TryRemove(AuctionRoomList.FirstOrDefault(r => r.Value == room).Key, out _);
                    if (_debug)
                    {
                        Logger.Info("Room " + room.Id + " deleted");
                    }
                }
                // otherwise set a new host and let other players know
                else
                {
                    var newHost = room.PlayerList.First();
                    newHost.SetHost(true);

                    using (var writer = DarkRiftWriter.Create())
                    {
                        writer.Write(id);
                        writer.Write(newHost.Id);
                        writer.Write(leaverName);

                        using (var msg = Message.Create(AuctionTags.PlayerLeft, writer))
                        {
                            foreach (var cl in room.Clients)
                            {
                                cl.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                    }
                }

                if (_debug)
                {
                    Logger.Info("User " + client.ID + " left room: " + room.Name);
                }
            }
            else
            {
                Logger.Warning("Tried to remove a player who wasn't in the room anymore");
            }
        }
        
        /// <summary>
        ///     Get the available auction rooms
        /// </summary>
        /// <param name="client">The connected client</param>
        private void GetOpenRooms(IClient client)
        {
            var availableRooms = AuctionRoomList.Values.Where(r => r.IsVisible && !r.HasStarted).ToList();

            using (var writer = DarkRiftWriter.Create())
            {
                foreach (var room in availableRooms)
                {
                    writer.Write(room);
                }

                using (var msg = Message.Create(AuctionTags.GetOpenRooms, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }
        
        /// <summary>
        ///     Start auction game request
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void StartAuction(IClient client, Message message)
        {
            ushort roomId = 0;

            try
            {
                using (var reader = message.GetReader())
                {
                    roomId = reader.ReadUInt16();
                }
            }
            catch (Exception ex)
            {
                // Return error 0 for invalid data packages received
                _loginPlugin.InvalidData(client, AuctionTags.StartAuctionFailed, ex, "Room join failed");
            }

            var username = _loginPlugin.GetPlayerUsername(client);
            var player = AuctionRoomList[roomId].PlayerList.FirstOrDefault(p => p.Name == username);

            if (player == null || !player.IsHost)
            {
                // Player isn't the host of this room -> return error 2
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write((byte) 2);

                    using (var msg = Message.Create(AuctionTags.StartAuctionFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }

                if (_debug)
                {
                    Logger.Info("User " + client.ID + " couldn't start the game, since he wasn't the host");
                }
                
                return;
            }
            
            // Start game
            AuctionRoomList[roomId].HasStarted = true;

            using (var msg = Message.CreateEmpty(AuctionTags.StartAuctionSuccess))
            {
                foreach (var cl in AuctionRoomList[roomId].Clients)
                {
                    cl.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        /// <summary>
        ///     Add a bid to the auction room
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void AddBid(IClient client, Message message)
        {
            ushort roomId = 0;
            uint newAmount = 0;
            
            try
            {
                using (var reader = message.GetReader())
                {
                    roomId = reader.ReadUInt16();
                    newAmount = reader.ReadUInt32();
                }
            }
            catch (Exception ex)
            {
                // Return error 0 for invalid data packages received
                _loginPlugin.InvalidData(client, AuctionTags.AddBidFailed, ex, "Room join failed");
            }
            
            var username = _loginPlugin.GetPlayerUsername(client);
            var room = AuctionRoomList[roomId];
            var player = room.PlayerList.FirstOrDefault(p => p.Name == username);
            
            // Add a new bid
            if (AuctionRoomList[roomId].AddBid(player.Id, newAmount, client))
            {
                // Send confirmation to the client
                using (var writer = DarkRiftWriter.Create())
                {
                    using (var msg = Message.Create(AuctionTags.AddBidSuccessful, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            
                // Let the other clients know
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(AuctionRoomList[roomId].LastBid);

                    using (var msg = Message.Create(AuctionTags.AddBid, writer))
                    {
                        foreach (var cl in room.Clients.Where(c => c.ID != client.ID))
                        {
                            cl.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }
            }
            else
            {
                // Send confirmation to the client
                using (var writer = DarkRiftWriter.Create())
                {
                    using (var msg = Message.Create(AuctionTags.AddBidFailed, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
        }
        
        #endregion

        #region Helpers

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="playerName"></param>
        /// <returns></returns>
        private static string AdjustAuctionRoomName(string roomName, string playerName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                return playerName + "'s Room";
            }

            return roomName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private ushort GenerateAuctionRoomId()
        {
            ushort i = 0;
            while (true)
            {
                if (!AuctionRoomList.ContainsKey(i))
                {
                    return i;
                }

                i++;
            }
        }

        #endregion

        #region Commands

        private void GetRoomsCommand(object sender, CommandEventArgs e)
        {
            
        }

        #endregion
    }
}