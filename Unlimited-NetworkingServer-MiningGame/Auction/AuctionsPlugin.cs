using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using MongoDB.Driver;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Game;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Mines;
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
            new Command("PlayersInRooms", "Show all players and joined rooms", "", GetPlayersInRoomsCommand),
            new Command("AddAuctions", "Add default auction rooms", "", AddDefaultAuctions),
        };

        public ConcurrentDictionary<uint, AuctionRoom> AuctionRoomList { get; } =
            new ConcurrentDictionary<uint, AuctionRoom>();
        private readonly ConcurrentDictionary<string, uint> _playersInRooms = new ConcurrentDictionary<string, uint>();
        private readonly ConcurrentDictionary<string, byte> _playerBids = new ConcurrentDictionary<string, byte>();

        private const string ConfigPath = @"Plugins/AuctionsPlugin.xml";
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
            var difference = Constants.InitialNrAuctions - nrAuctionRooms;
            if (AuctionRoomList != null && nrAuctionRooms < Constants.InitialNrAuctions)
            {
                for(int i = 0; i < difference; i++)
                {
                    GenerateAuctionRoom(i * 60);
                }
            }

            _loginPlugin.OnLogout += OnUserLeft;
        }
        
        public AuctionsPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            LoadConfig();
            ClientManager.ClientConnected += OnPlayerConnected;
            ClientManager.ClientDisconnected += OnPlayerDisconnected;
        }

        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            e.Client.MessageReceived += OnMessageReceived;
        }

        private void OnPlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
        { }
        
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
                        var username = _loginPlugin.GetPlayerUsername(client);
                        LeaveAuctionRoom(client, username);
                        break;
                    }

                    case AuctionTags.GetRoom:
                    {
                        GetRoom(client);
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
                        // TODO
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
                _loginPlugin.InvalidData(client, AuctionTags.CreateFailed, ex, "Auction create failed");
                return;
            }

            var username = _loginPlugin.GetPlayerUsername(client);
            roomName = AdjustAuctionRoomName(roomName, username);
            var roomId = GenerateAuctionRoomId();
            var room = new AuctionRoom(roomId, roomName);          
            var player = new Player(_loginPlugin.GetPlayerUsername(client), true);

            room.AddPlayer(player, client);
            AuctionRoomList.TryAdd(roomId, room);
            _playersInRooms.TryAdd(username, roomId);

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
                Logger.Info("Creating auction: " + room.Name);
            }
        }
        
        /// <summary>
        ///     Join an available auction room
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void JoinAuctionRoom(IClient client, Message message)
        {
            uint roomId = 0;

            try
            {
                using (var reader = message.GetReader())
                {
                    roomId = reader.ReadUInt32();
                }
            }
            catch (Exception ex)
            {
                // Return error 0 for invalid data packages received
                _loginPlugin.InvalidData(client, AuctionTags.JoinFailed, ex, "Auction join failed");
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
                    Logger.Warning("Room join failed! Room " + roomId + " doesn't exist anymore");
                }

                return;
            }

            var username = _loginPlugin.GetPlayerUsername(client);
            var room = AuctionRoomList[roomId];
            var newPlayer = new Player(username, false);

            // Check if player already is in an active room -> send error 2
            if(_playersInRooms.ContainsKey(username))
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
                    Logger.Warning("User " + _loginPlugin.GetPlayerUsername(client) + " couldn't join Room " + room.Id +
                                ", since he already is in Room: " + _playersInRooms[username]);
                }
            }

            // Try to join room
            if (room.AddPlayer(newPlayer, client))
            {
                _playersInRooms[username] = roomId;

                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(room);
                    writer.Write(room.MineScans.Where(s => s.Player == username).ToArray());
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
                    Logger.Warning("User " + client.ID + " couldn't join, since Room " + room.Id + " is full");
                }
            }
        }

        /// <summary>
        ///     Leave an auction room
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="username">The user name</param>
        private void LeaveAuctionRoom(IClient client, string username)
        {
            if(!_playersInRooms.ContainsKey(username)) return;

            var room = AuctionRoomList[_playersInRooms[username]];
            _playersInRooms.TryRemove(username, out _);

            if (room.RemovePlayer(client, username))
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
                
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(username);

                    using (var msg = Message.Create(AuctionTags.PlayerLeft, writer))
                    {
                        foreach (var cl in room.Clients)
                        {
                            cl.SendMessage(msg, SendMode.Reliable);
                        }
                    }
                }
            }
            else
            {
                Logger.Warning("Tried to remove player " + username + " who isn't in the room anymore");
            }
        }

        /// <summary>
        ///     Get an available auction room
        /// </summary>
        /// <param name="client"></param>
        private void GetRoom(IClient client)
        {
            AuctionRoom room = null;
            var username = _loginPlugin.GetPlayerUsername(client);
            try
            {
                if (!_playersInRooms.ContainsKey(username))
                {
                    room = AuctionRoomList.Values.FirstOrDefault(r => r.HasStarted);
                    TimeSpan difference = DateTime.FromBinary(room.EndTime) - DateTime.UtcNow;
                    if (difference.TotalSeconds < 60)
                    {
                        if (AuctionRoomList.ContainsKey(room.Id + 1))
                        {
                            room = AuctionRoomList[room.Id + 1];
                        }
                    }
                }
                else
                {
                    uint latestRoomId = _playersInRooms[username];

                    uint newId = latestRoomId + 1;
                    bool isFound = false;
                    while (AuctionRoomList.ContainsKey(newId) && !isFound)
                    {
                        TimeSpan difference = DateTime.FromBinary(AuctionRoomList[newId].EndTime) - DateTime.UtcNow;
                        if (difference.TotalSeconds > 60 &&
                            AuctionRoomList[newId].HasStarted)
                        {
                            isFound = true;
                            room = AuctionRoomList[newId];
                        }

                        newId++;
                    }

                    if (!isFound)
                    {
                        room = AuctionRoomList.Values.FirstOrDefault(r => r.HasStarted);
                    }
                }
            }
            catch
            {
                using (var msg = Message.CreateEmpty(AuctionTags.GetRoomFailed))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }

            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(room);
                try
                {
                    writer.Write(_playerBids[username]);
                }
                catch
                {
                    writer.Write((byte) 0);
                }

                using (var msg = Message.Create(AuctionTags.GetRoom, writer))
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
            uint roomId = 0;

            try
            {
                using (var reader = message.GetReader())
                {
                    roomId = reader.ReadUInt32();
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
                    Logger.Warning("User " + username + " couldn't start the game, since he wasn't the host");
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

            AuctionRoomList[roomId].OnAuctionFinished += AuctionFinishedHandler;
        }

        /// <summary>
        ///     Add a bid to the auction room
        /// </summary>
        /// <param name="client">The connected client</param>
        /// <param name="message">The message received</param>
        private void AddBid(IClient client, Message message)
        {
            uint roomId = 0;
            uint newAmount = 0;
            uint newEnergy = 0;
            
            try
            {
                using (var reader = message.GetReader())
                {
                    roomId = reader.ReadUInt32();
                    newAmount = reader.ReadUInt32();
                    newEnergy = reader.ReadUInt32();
                }
            }
            catch (Exception ex)
            {
                // Return error 0 for invalid data packages received
                _loginPlugin.InvalidData(client, AuctionTags.AddBidFailed, ex, "Room join failed");
            }
            
            var username = _loginPlugin.GetPlayerUsername(client);
            var room = AuctionRoomList[roomId];
            var overbiddedPlayer = room.LastBid.PlayerName;

            // Add a new bid
            if (AuctionRoomList[roomId].AddBid(username, newAmount, client))
            {
                _database.DataLayer.AddBid(roomId, AuctionRoomList[roomId].LastBid, () => { });

                if (_playerBids.ContainsKey(username))
                {
                    _playerBids[username]++;
                }
                else
                {
                    _playerBids[username] = 1;
                }
                
                // Send confirmation to the client
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(AuctionRoomList[roomId].LastBid);
                    writer.Write(newAmount);

                    using (var msg = Message.Create(AuctionTags.AddBidSuccessful, writer))
                    {
                        client.SendMessage(msg, SendMode.Reliable);
                    }
                    
                    // Set the new energy in the database
                    _database.DataLayer.SetPlayerEnergy(username, newEnergy, () => { });
                }
                
                // Let the other clients know
                using (var writer = DarkRiftWriter.Create())
                {
                    writer.Write(AuctionRoomList[roomId].LastBid);

                    if (room.OverbiddedClient != null)
                    {
                        using (var msg = Message.Create(AuctionTags.AddBid, writer))
                        {
                            foreach (var cl in room.Clients.Where(c => c.ID != client.ID && c.ID != room.OverbiddedClient.ID))
                            {
                                cl.SendMessage(msg, SendMode.Reliable);
                            }
                        }
                        
                        writer.Write(Constants.IncrementBid);
                        
                        using (var msg = Message.Create(AuctionTags.Overbid, writer))
                        {
                            if (room.OverbiddedClient != null && !room.Clients.Contains(room.OverbiddedClient))
                            {
                                room.OverbiddedClient.SendMessage(msg, SendMode.Reliable);
                            }
                        }

                        _playerBids[overbiddedPlayer]--;
                        
                        // Store energy in the database
                        var overbiddedUsername = _loginPlugin.GetPlayerUsername(room.OverbiddedClient);
                        _database.DataLayer.IncreasePlayerEnergy(overbiddedUsername, Constants.IncrementBid, () => { });
                    }
                    else
                    {
                        using (var msg = Message.Create(AuctionTags.AddBid, writer))
                        {
                            foreach (var cl in room.Clients.Where(c => c.ID != client.ID))
                            {
                                cl.SendMessage(msg, SendMode.Reliable);
                            }
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
            uint roomId = 0;
            MineScan mineScan = new MineScan();
            
            try
            {
                using (var reader = message.GetReader())
                {
                    roomId = reader.ReadUInt32();
                    mineScan = reader.ReadSerializable<MineScan>();
                }
            }
            catch (Exception ex)
            {
                // Return error 0 for invalid data packages received
                _loginPlugin.InvalidData(client, AuctionTags.AddScanFailed, ex, "Add scan failed");
            }
            
            var username = _loginPlugin.GetPlayerUsername(client);
            mineScan.Player = username;

            // Add a new scan
            if (AuctionRoomList[roomId].AddScan(mineScan, client, username))
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

        /// <summary>
        ///     Auction room finished event
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The auction finished event args</param>
        private void AuctionFinishedHandler(object sender, AuctionFinishedEventArgs e)
        {
            AuctionFinished(e.AuctionId, e.Name, e.Owner, true);
        }

        /// <summary>
        ///     On user left actions
        /// </summary>
        /// <param name="client">The client connected</param>
        /// <param name="username">The player username</param>
        private void OnUserLeft(IClient client, string username)
        {
            LeaveAuctionRoom(client, username);
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
                    _latestRoomKey = 1;
                }
                else
                {
                    _latestRoomKey++;
                }
            }
            catch (InvalidOperationException)
            {
                _latestRoomKey = 0;
            }

            return _latestRoomKey;
        }

        /// <summary>
        ///     Load the configuration file
        /// </summary>
        private void LoadConfig()
        {
            XDocument document;

            if (!File.Exists(ConfigPath))
            {
                document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("Settings for the Auctions Plugin"),
                    new XElement("Variables", new XAttribute("Debug", true))
                );
                try
                {
                    document.Save(ConfigPath);
                    Logger.Info("Created /Plugins/AuctionsPlugin.xml!");
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to create AuctionsPlugin.xml: " + ex.Message + " - " + ex.StackTrace);
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
                    Logger.Error("Failed to load AuctionsPlugin.xml: " + ex.Message + " - " + ex.StackTrace);
                }
            }
        }
        
        #endregion

        #region AuctionsGeneration

        /// <summary>
        ///     Restore the available auction rooms from the database
        /// </summary>
        private void RestoreAuctionRooms()
        {
            _database.DataLayer.GetAuctions(savedAuctions =>
            {
                _latestRoomKey = 0;
                foreach (var auctionRoom in savedAuctions)
                {
                    if (auctionRoom.Id > _latestRoomKey)
                    {
                        _latestRoomKey = auctionRoom.Id;
                    }
                    AuctionRoomList.TryAdd(auctionRoom.Id, auctionRoom);
                    if (auctionRoom.EndTime != 0 &&
                        DateTime.Compare(DateTime.FromBinary(auctionRoom.EndTime), DateTime.UtcNow) < 0)
                    {
                        AuctionFinished(auctionRoom.Id, auctionRoom.Name, auctionRoom.LastBid.PlayerName, false);
                    }
                    else
                    {
                        if (_debug)
                        {
                            Logger.Info("Auction restored: " + auctionRoom.Name + " (ends " + DateTime.FromBinary(auctionRoom.EndTime) + ")");
                        }
                        AuctionRoomList[auctionRoom.Id].ContinueAuction();
                        AuctionRoomList[auctionRoom.Id].OnAuctionFinished += AuctionFinishedHandler;
                        if (auctionRoom.LastBid.Id > 0)
                        {
                            if(_playerBids.ContainsKey(auctionRoom.LastBid.PlayerName))
                            {
                                _playerBids.TryAdd(auctionRoom.LastBid.PlayerName, _playerBids[auctionRoom.LastBid.PlayerName]++);
                            }
                            else
                            {
                                _playerBids.TryAdd(auctionRoom.LastBid.PlayerName, 1);
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        ///     Generates a new auction room
        /// </summary>
        /// <param name="delay">The delay for starting the auction</param>
        private void GenerateAuctionRoom(int delay = 0)
        {
            var roomId = GenerateAuctionRoomId();
            var roomName = NameGenerator.RandName();
            var room = new AuctionRoom(roomId, roomName);
            
            room.StartAuction(delay);

            bool addedSuccess = false;
            while(!addedSuccess)
            {
                try
                {
                    _database.DataLayer.AddAuction(room, () =>
                    { });
                    addedSuccess = true;
                }
                catch (MongoDuplicateKeyException)
                {
                    roomId++;
                    room.Id = roomId;
                    room.StartAuction(delay);
                }
            }
            
            room.OnAuctionFinished += AuctionFinishedHandler;

            AuctionRoomList.TryAdd(roomId, room);

            if (_debug)
            {
                Logger.Info("Creating auction: " + roomId + ": " + room.Name+ " (ends " + DateTime.FromBinary(room.EndTime) + ")");
            }
        }

        /// <summary>
        ///     Finishes an auction
        /// </summary>
        /// <param name="auctionId">The auction id</param>
        /// <param name="auctionName">The auction name</param>
        /// <param name="auctionOwner">The auction owner</param>
        /// <param name="generateNew">True to generate a new auction and false otherwise</param>
        private void AuctionFinished(uint auctionId, string auctionName, string auctionOwner, bool generateNew)
        {
            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write(auctionId);
                writer.Write(AuctionRoomList[auctionId].LastBid.PlayerName);
                
                using (var msg = Message.Create(AuctionTags.AuctionFinished, writer))
                {
                    foreach (var client in AuctionRoomList[auctionId].Clients)
                    {
                        var username = _loginPlugin.GetPlayerUsername(client);
                        if (client.ConnectionState == ConnectionState.Connected) client.SendMessage(msg, SendMode.Reliable);
                        _playersInRooms.TryRemove(username, out _);
                    }
                    
                    if (AuctionRoomList[auctionId].LastBidderClient != null &&
                        AuctionRoomList[auctionId].LastBidderClient.ConnectionState == ConnectionState.Connected)
                    {
                        AuctionRoomList[auctionId].LastBidderClient.SendMessage(msg, SendMode.Reliable);
                    }
                    // TODO: Add queued task that is sent to the user when they are online (on else)
                }
            }

            if (AuctionRoomList[auctionId].LastBid.Id > 0)
            {
                Mine mine = new Mine(auctionId, auctionName, auctionOwner);
                _database.DataLayer.AddMine(mine, () => { });
                _playerBids[AuctionRoomList[auctionId].LastBid.PlayerName]--;
            }

            if (!AuctionRoomList.TryRemove(auctionId, out _))
            {
                Logger.Error("Could not remove auction: " + auctionId);
            }

            Logger.Info("Finished auction: " + auctionName);

            _database.DataLayer.RemoveAuction(auctionId, () => { });
            
            if (generateNew)
            {
                GenerateAuctionRoom(60 * (Constants.InitialNrAuctions + 1));
            }
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
            var rooms = AuctionRoomList.Values.ToList();
            foreach (var room in rooms)
            {
                Logger.Info(room.Name + " [" + room.Id + "] - " + room.PlayerList.Count + "/" + Constants.MaxAuctionPlayers);
            }
        }
        
        private void GetPlayersInRoomsCommand(object sender, CommandEventArgs e)
        {
            Logger.Info("Players in rooms: ");

            foreach (var player in _playersInRooms)
            {
                Logger.Info(player.Key + ": " + player.Value);
            }
        }

        /// <summary>
        ///     Add the default auction rooms in memory and in database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void AddDefaultAuctions(object sender, CommandEventArgs e)
        {
            for (int i = 0; i < 6; i++)
            {
                GenerateAuctionRoom(i * 60);
            }
        }

        #endregion
    }
}