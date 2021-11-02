using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Game;
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
            new Command("AuctionRooms", "Show all auction rooms", "", GetRoomsCommand),
            new Command("AddAuctions", "Add default auction rooms", "", AddDefaultAuctions),

            #region Testcommands
            new Command("StartAuctionTest", "Test for auction timers", "", StartAuctionTest),
            new Command("AddAuctionTest", "Test for adding an auction to the database", "", AddAuctionTest),
            new Command("AddMineTest", "Test for adding a mine to the database", "", AddMineTest),
            new Command("RestoreAuctionsTest", "Test for restoring auctions from the database", "", RestoreAuctionsTest)
            #endregion
        };

        public ConcurrentDictionary<uint, AuctionRoom> AuctionRoomList { get; } =
            new ConcurrentDictionary<uint, AuctionRoom>();
        private readonly ConcurrentDictionary<uint, uint> _playersInRooms =
            new ConcurrentDictionary<uint, uint>();

        private LoginPlugin _loginPlugin;
        private DatabaseProxy _database;
        private uint _latestRoomKey;
        private bool _debug = true;

        protected override void Loaded(LoadedEventArgs args)
        {
            if (_database == null) _database = PluginManager.GetPluginByType<DatabaseProxy>();
            if (_loginPlugin == null) _loginPlugin = PluginManager.GetPluginByType<LoginPlugin>();
            
            RestoreAuctionRooms();

            var nrAuctionRooms = (uint) AuctionRoomList.Count;
            var difference = (uint) (Constants.InitialNrAuctions - nrAuctionRooms);
            if (AuctionRoomList != null && nrAuctionRooms < Constants.InitialNrAuctions)
            {
                Logger.Info("Not enough mines, creating another " + difference + " mines");
                GenerateAuctionRooms(difference);                
            }

            try
            {
                _latestRoomKey = AuctionRoomList.Keys.Max();    
            }
            catch (InvalidOperationException e)
            {
                _latestRoomKey = 0;
            }
        }
        
        public AuctionsPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }

        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
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
                if (!_loginPlugin.PlayerLoggedIn(client, AuctionTags.RequestFailed, "Player not logged in."))
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

                    case AuctionTags.AddScan:
                    {
                        AddScan(client, message);
                        break;
                    }

                    case AuctionTags.AddFriendToAuction:
                    {
                        // TO-DO
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

            try
            {
                using (var reader = message.GetReader())
                {
                    roomName = reader.ReadString();
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
            var room = new AuctionRoom(roomId, roomName);          
            var player = new Player(client.ID, _loginPlugin.GetPlayerUsername(client), true);

            room.AddPlayer(player, client);
            AuctionRoomList.TryAdd(roomId, room);
            _playersInRooms.TryAdd(client.ID, roomId);

            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(room);
                writer.Write(player);

                using (var msg = Message.Create(AuctionTags.CreateSuccess, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
                
                // Add auction room to the database
                _database.DataLayer.AddAuction(room, () => {});
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
                _playersInRooms[client.ID] = roomId;

                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(room);
                    writer.Write(room.MineScans);

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

            var room = AuctionRoomList[_playersInRooms[id]];
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
            Logger.Info("Getting open rooms");
            var availableRooms = AuctionRoomList.Values.Where(r => r.HasStarted).ToList();

            using (var writer = DarkRiftWriter.Create())
            {
                foreach (var room in availableRooms)
                {
                    writer.Write(room);
                    Logger.Info("Sending room: " + room.Id);
                }

                using (var msg = Message.Create(AuctionTags.GetOpenRooms, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
            
            Logger.Info("Finished getting open rooms");
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
            AuctionRoomList[roomId].StartAuction(0);

            using (var msg = Message.CreateEmpty(AuctionTags.StartAuctionSuccess))
            {
                foreach (var cl in AuctionRoomList[roomId].Clients)
                {
                    cl.SendMessage(msg, SendMode.Reliable);
                }
            }

            AuctionRoomList[roomId].OnAuctionFinished += AuctionFinished;
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
            if (player != null && AuctionRoomList[roomId].AddBid(player.Id, AuctionRoomList[player.Id].GetPlayerUsername(player.Id), newAmount, client))
            {
                // Send confirmation to the client
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(AuctionRoomList[roomId].LastBid);

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
                // Send add fail to the client
                using (var msg = Message.CreateEmpty(AuctionTags.AddBidFailed))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }
        
        /// <summary>
        ///     Add a scan to the auction mine
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void AddScan(IClient client, Message message)
        {
            ushort roomId = 0;
            MineScan mineScan = new MineScan();
            
            try
            {
                using (var reader = message.GetReader())
                {
                    roomId = reader.ReadUInt16();
                    mineScan = reader.ReadSerializable<MineScan>();
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

            // Add a new scan
            if (player != null && AuctionRoomList[roomId].AddScan(mineScan, client))
            {
                _database.DataLayer.AddScan(roomId, mineScan, () => {});
            }
            else
            {
                // Send add failed to the client
                using (var msg = Message.CreateEmpty(AuctionTags.AddScanFailed))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }
        
        #endregion

        #region Events

        private void AuctionFinished(object sender, AuctionFinishedEventArgs e)
        {
            var winner = AuctionRoomList[e.AuctionId].LastBid.UserId;
            
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(e.AuctionId);
                writer.Write(winner);

                using (var msg = Message.Create(AuctionTags.AuctionFinished, writer))
                {
                    foreach (var client in AuctionRoomList[e.AuctionId].Clients)
                    {
                        AuctionRoomList[e.AuctionId].RemovePlayer(client);
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                }
            }
            
            if (AuctionRoomList[e.AuctionId].LastBid.Id > 0)
            {
                MineData mine = new MineData(e.AuctionId, e.Name, e.Owner);
                _database.DataLayer.AddMine(mine, () => { });   
            }
            
            AuctionRoomList.TryRemove(AuctionRoomList.FirstOrDefault(r => r.Key == e.AuctionId).Key, out _);
            
            _database.DataLayer.RemoveAuction(e.AuctionId, () => { });

            if (_debug)
            {
                Logger.Info("Auction " + e.AuctionId + " finished");
            }
            
            GenerateAuctionRooms(1);
        }

        #endregion

        #region Helpers

        /// <summary>
        ///     Sets the auction room name
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
        ///     Generates a new id for the auction room
        /// </summary>
        /// <returns></returns>
        private uint GenerateAuctionRoomId()
        {
            try
            {
                if (_latestRoomKey >= 4294967290)
                {
                    _latestRoomKey = 0;
                }
                else
                {
                    _latestRoomKey++;
                }
            }
            catch (InvalidOperationException e)
            {
                _latestRoomKey = 0;
            }

            return _latestRoomKey;
        }

        #endregion

        #region AuctionsGeneration

        private void RestoreAuctionRooms()
        {
            _database.DataLayer.GetAuctions(savedAuctions =>
            {
                foreach (var auctionRoom in savedAuctions)
                {
                    if (auctionRoom.EndTime != 0 &&
                        DateTime.Compare(DateTime.FromBinary(auctionRoom.EndTime), DateTime.Now) < 0)
                    {
                        // TO-DO
                        // Actions needed before deleting the auction
                        
                        Logger.Info("Auction deleted " + auctionRoom.Id);
                        _database.DataLayer.RemoveAuction(auctionRoom.Id, () => {});
                    }
                    else
                    {
                        Logger.Info("Auction restored " + auctionRoom.Id);
                        AuctionRoomList.TryAdd(auctionRoom.Id, auctionRoom);
                        AuctionRoomList[auctionRoom.Id].OnAuctionFinished += AuctionFinished;
                    }
                }
            });
        }
        
        private void GenerateAuctionRooms(uint nrAuctions)
        {
            for (ushort i = 0; i < nrAuctions; i++)
            {
                var roomId = GenerateAuctionRoomId();
                var roomName = NameGenerator.RandName();
                var room = new AuctionRoom(roomId, roomName);
                
                room.StartAuction(i);
                room.OnAuctionFinished += AuctionFinished;
                
                AuctionRoomList.TryAdd(roomId, room);
                
                _database.DataLayer.AddAuction(room, () => {});

                if (_debug)
                {
                    Logger.Info("Creating auction room " + roomId + ": " + room.Name);
                }   
            }
        }

        private void AddScanTest()
        {
            MineScan mineScan = new MineScan(1, 12, 13);
            _database.DataLayer.AddScan(1, mineScan, () => {});
        }

        #endregion

        #region Commands

        /// <summary>
        ///     Command for showing all the available rooms
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void GetRoomsCommand(object sender, CommandEventArgs e)
        {
            Logger.Info("Active rooms: ");

            var rooms = AuctionRoomList.Values.ToList();
            foreach (var room in rooms)
            {
                Logger.Info(room.Name + " [" + room.Id + "] - " + room.PlayerList.Count + "/" + Constants.MaxAuctionPlayers);
            }
        }
        
        /// <summary>
        ///     Add the default auction rooms in memory and in database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void AddDefaultAuctions(object sender, CommandEventArgs e)
        {
            GenerateAuctionRooms(6);
        }
        
        private void StartAuctionTest(object sender, CommandEventArgs e)
        {
            var roomId = GenerateAuctionRoomId();
            var room = new AuctionRoom(roomId, "test");
            
            room.StartAuction(0);

            room.OnAuctionFinished += AuctionFinished;
        }
        
        private void AddAuctionTest(object sender, CommandEventArgs e)
        {
            ushort id = 21;
            var room = new AuctionRoom(id, "test");
            room.EndTime = DateTime.Now.ToBinary();

            _database.DataLayer.AddAuction(room, () => {});
            
            Logger.Info("Added auction to the database");
        }
        
        private void AddMineTest(object sender, CommandEventArgs e)
        {
            MineData mine = new MineData(1, "test", "gigel");

            _database.DataLayer.AddMine(mine, () => {});
            
            Logger.Info("Added mine to the database");
        }

        private void RestoreAuctionsTest(object sender, CommandEventArgs e)
        {
            RestoreAuctionRooms();
        }

        #endregion
    }
}