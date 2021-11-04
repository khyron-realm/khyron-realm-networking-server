using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Unlimited_NetworkingServer_MiningGame.Auction;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Friends;
using Unlimited_NetworkingServer_MiningGame.Game;
using Unlimited_NetworkingServer_MiningGame.Headquarters;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Mines;

namespace Unlimited_NetworkingServer_MiningGame.MongoDbConnector
{
    /// <summary>
    ///     Data layer class that contains implementations for database operations
    /// </summary>
    internal class DataLayer : IDataLayer
    {
        private readonly MongoDbPlugin _database;

        public DataLayer(string name, MongoDbPlugin database)
        {
            Name = name;
            _database = database;
        }

        public string Name { get; }

        #region Login

        /// <inheritdoc />
        public async void GetUser(string username, Action<IUser> callback)
        {
            var user = await _database.Users.Find(u => u.Username == username).FirstOrDefaultAsync();
            callback(user);
        }

        /// <inheritdoc />
        public async void UsernameAvailable(string username, Action<bool> callback)
        {
            callback(await _database.Users.Find(u => u.Username == username).FirstOrDefaultAsync() == null);
        }

        /// <inheritdoc />
        public async void AddNewUser(string username, string password, Action callback)
        {
            await _database.Users.InsertOneAsync(new User(username, password));
            callback();
        }

        /// <inheritdoc />
        public async void DeleteUser(string username, Action callback)
        {
            await _database.Users.DeleteOneAsync(u => u.Username == username);
            callback();
        }

        #endregion

        #region Headquarters

        /// <inheritdoc />
        public async void AddPlayerData(PlayerData player, Action callback)
        {
            await _database.PlayerData.InsertOneAsync(player);
            callback();
        }
        
        /// <inheritdoc />
        public async void GetPlayerData(string username, Action<PlayerData> callback)
        {
            PlayerData playerData = await _database.PlayerData.Find(u => u.Id == username).FirstOrDefaultAsync();
            callback(playerData);
        }

        /// <inheritdoc />
        public async void GetPlayerLevel(string username, Action<uint> callback)
        {
            var level = await _database.PlayerData.Find(u => u.Id == username).Project(u => u.Level)
                .FirstOrDefaultAsync();
            callback(level);
        }
        
        /// <inheritdoc />
        public async void SetPlayerLevel(string username, byte level, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.Level, level);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public async void SetPlayerLevelExperience(string username, byte level, uint experience, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.Level, level).Set(u => u.Experience, experience);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void GetPlayerExperience(string username, Action<uint> callback)
        {
            var experience = await _database.PlayerData.Find(u => u.Id == username).Project(u => u.Experience)
                .FirstOrDefaultAsync();
            callback(experience);
        }

        /// <inheritdoc />
        public async void SetPlayerExperience(string username, uint experience, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.Experience, experience);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void GetPlayerEnergy(string username, Action<uint> callback)
        {
            var energy = await _database.PlayerData.Find(u => u.Id == username).Project(u => u.Energy).FirstOrDefaultAsync();
            callback(energy);
        }

        /// <inheritdoc />
        public async void SetPlayerEnergy(string username, uint energy, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.Energy, energy);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public async void GetPlayerResources(string username, Action<Resource[]> callback)
        {
            var resources = await _database.PlayerData.Find(u => u.Id == username).Project(u => u.Resources).FirstOrDefaultAsync();
            callback(resources);
        }

        /// <inheritdoc />
        public async void SetPlayerResources(string username, Resource[] resources, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.Resources, resources);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void GetPlayerRobots(string username, Action<Robot[]> callback)
        {
            var robots = await _database.PlayerData.Find(u => u.Id == username).Project(u => u.Robots).FirstOrDefaultAsync();
            callback(robots);
        }

        /// <inheritdoc />
        public async void SetPlayerRobot(string username, byte robotId, Robot robot, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.And(
                Builders<PlayerData>.Filter.Eq(u => u.Id, username),
                Builders<PlayerData>.Filter.ElemMatch(u => u.Robots, r => r.Id == robotId));
            var update = Builders<PlayerData>.Update.Set(b => b.Robots[-1], robot);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public async void SetPlayerRobots(string username, Robot[] robots, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.Robots, robots);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void AddTask(TaskType taskType, string username, ushort id, byte element, long time, Action callback)
        {
            var upgrade = new BuildTask(id, element, time);
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            UpdateDefinition<PlayerData> update = null;
            switch (taskType)
            {
                case TaskType.Conversion:
                {
                    update = Builders<PlayerData>.Update.AddToSet(u => u.ConversionQueue, upgrade);
                    break;
                }
                case TaskType.Upgrade:
                {
                    update = Builders<PlayerData>.Update.AddToSet(u => u.UpgradeQueue, upgrade);
                    break;
                }
                case TaskType.Build:
                {
                    update = Builders<PlayerData>.Update.AddToSet(u => u.BuildQueue, upgrade);
                    break;
                }
            }
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void FinishTask(TaskType taskType, string username, ushort id, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            UpdateDefinition<PlayerData> update = null;
            switch (taskType)
            {
                case TaskType.Conversion:
                {
                    update = Builders<PlayerData>.Update.PullFilter(u => u.ConversionQueue,
                            Builders<BuildTask>.Filter.Eq(b => b.Id, id));
                    break;
                }
                case TaskType.Upgrade:
                {
                    update = Builders<PlayerData>.Update.PullFilter(u => u.UpgradeQueue,
                            Builders<BuildTask>.Filter.Eq(b => b.Id, id));
                    break;
                }
                case TaskType.Build:
                {
                    update = Builders<PlayerData>.Update.PullFilter(u => u.BuildQueue,
                            Builders<BuildTask>.Filter.Lte(b => b.Id, id));
                    break;
                }
            }
            await _database.PlayerData.UpdateManyAsync(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public async void UpdateNextTask(TaskType taskType, string username, ushort id, long time, Action callback)
        {
            FilterDefinition<PlayerData> filter = null;
            UpdateDefinition<PlayerData> update = null;
            switch (taskType)
            {
                case TaskType.Conversion:
                {
                    filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username) &
                             Builders<PlayerData>.Filter.ElemMatch(u => u.ConversionQueue, b => b.Id == id);
                    update = Builders<PlayerData>.Update.Set(b => b.ConversionQueue[-1].StartTime, time);
                    break;
                }
                case TaskType.Upgrade:
                {
                    filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username) &
                             Builders<PlayerData>.Filter.ElemMatch(u => u.UpgradeQueue, b => b.Id == id);
                    update = Builders<PlayerData>.Update.Set(b => b.UpgradeQueue[-1].StartTime, time);
                    break;
                }
                case TaskType.Build:
                {
                    filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username) &
                             Builders<PlayerData>.Filter.ElemMatch(u => u.BuildQueue, b => b.Id > id);
                    update = Builders<PlayerData>.Update.Set(b => b.BuildQueue[-1].StartTime, time);
                    break;
                }
            }
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        #endregion

        #region Parameters

        /// <inheritdoc />
        public async void AddGameData(GameData data, Action callback)
        {
            await _database.GameData.InsertOneAsync(data);
            callback();
        }
        
        /// <inheritdoc />
        public async void GetGameData(Action<GameData> callback)
        {
            var filter = Builders<GameData>.Filter.Empty;
            var sort = Builders<GameData>.Sort.Descending(p => p.Version);
            var gameData = await _database.GameData.Find(filter).Sort(sort).FirstOrDefaultAsync();
            callback(gameData);
        }

        /// <inheritdoc />
        public async void GetGameData(ushort version, Action<GameData> callback)
        {
            var gameData = await _database.GameData.Find(p => p.Version == version).FirstOrDefaultAsync();
            callback(gameData);
        }
        
        #endregion

        #region Friends
        
        /// <inheritdoc />
        public async void AddRequest(string sender, string receiver, Action callback)
        {
            //Add OpenFriendRequest of sender to receiver
            var updateReceiving = Builders<FriendList>.Update.AddToSet(u => u.OpenFriendRequests, sender);
            var task1 = _database.FriendList.UpdateOneAsync(u => u.Username == receiver, updateReceiving);

            //Add OpenFriendRequest of receiver to sender
            var updateSender = Builders<FriendList>.Update.AddToSet(u => u.UnansweredFriendRequests, receiver);
            var task2 = _database.FriendList.UpdateOneAsync(u => u.Username == sender, updateSender);

            await Task.WhenAll(task1, task2);
            callback();
        }

        /// <inheritdoc />
        public async void RemoveRequest(string sender, string receiver, Action callback)
        {
            //Remove OpenFriendRequest of receiver from sender
            var updateSender = Builders<FriendList>.Update.Pull(u => u.OpenFriendRequests, receiver);
            var task1 = _database.FriendList.UpdateOneAsync(u => u.Username == sender, updateSender);

            //Remove OpenFriendRequest of sender from receiver
            var updateReceiving = Builders<FriendList>.Update.Pull(u => u.UnansweredFriendRequests, sender);
            var task2 = _database.FriendList.UpdateOneAsync(u => u.Username == receiver, updateReceiving);

            await Task.WhenAll(task1, task2);
            callback();
        }

        /// <inheritdoc />
        public async void AddFriend(string sender, string receiver, Action callback)
        {
            var tasks = new List<Task>();
            //Add sender to receivers friend list
            var updateReceiving = Builders<FriendList>.Update.AddToSet(u => u.Friends, sender);
            tasks.Add(_database.FriendList.UpdateOneAsync(u => u.Username == receiver, updateReceiving));

            //Add receiver to senders friend list
            var updateSending = Builders<FriendList>.Update.AddToSet(u => u.Friends, receiver);
            tasks.Add(_database.FriendList.UpdateOneAsync(u => u.Username == sender, updateSending));

            //Remove OpenFriendRequest of receiver from sender
            var updateSender = Builders<FriendList>.Update.Pull(u => u.OpenFriendRequests, receiver);
            tasks.Add(_database.FriendList.UpdateOneAsync(u => u.Username == sender, updateSender));

            //Remove OpenFriendRequest of sender from receiver
            var updateReceiver = Builders<FriendList>.Update.Pull(u => u.UnansweredFriendRequests, sender);
            tasks.Add(_database.FriendList.UpdateOneAsync(u => u.Username == receiver, updateReceiver));

            await Task.WhenAll(tasks);
            callback();
        }

        /// <inheritdoc />
        public void RemoveFriend(string sender, string receiver, Action callback)
        {
            GetFriendLists(new[] {sender, receiver}, async friendLists =>
            {
                var senderUser = friendLists.Single(u => u.Username == sender);
                var receiverUser = friendLists.Single(u => u.Username == receiver);

                var tasks = new List<Task>();

                //Update sender
                if (senderUser.Friends.Contains(receiver))
                {
                    //remove receiver from senders friend list
                    var updateSender = Builders<FriendList>.Update.Pull(u => u.Friends, receiver);
                    tasks.Add(_database.FriendList.UpdateOneAsync(u => u.Username == sender, updateSender));
                }
                if (senderUser.OpenFriendRequests.Contains(receiver))
                {
                    //remove receiver from senders open friend requests
                    var updateSender = Builders<FriendList>.Update.Pull(u => u.OpenFriendRequests, receiver);
                    tasks.Add(_database.FriendList.UpdateOneAsync(u => u.Username == sender, updateSender));
                }
                if (senderUser.UnansweredFriendRequests.Contains(receiver))
                {
                    // remove receiver from senders unanswered friend requests
                    var updateSender = Builders<FriendList>.Update.Pull(u => u.UnansweredFriendRequests, receiver);
                    tasks.Add(_database.FriendList.UpdateOneAsync(u => u.Username == sender, updateSender));
                }

                //Update receiver
                if (receiverUser.Friends.Contains(sender))
                {
                    //remove sender from receivers friend list
                    var updateReceiver = Builders<FriendList>.Update.Pull(u => u.Friends, sender);
                    tasks.Add(_database.FriendList.UpdateOneAsync(u => u.Username == receiver, updateReceiver));
                }
                if (receiverUser.OpenFriendRequests.Contains(sender))
                {
                    //remove sender from receivers open friend requests
                    var updateReceiver = Builders<FriendList>.Update.Pull(u => u.OpenFriendRequests, sender);
                    tasks.Add(_database.FriendList.UpdateOneAsync(u => u.Username == receiver, updateReceiver));
                }
                if (receiverUser.UnansweredFriendRequests.Contains(sender))
                {
                    //remove sender from receivers unanswered friend requests
                    var updateReceiver = Builders<FriendList>.Update.Pull(u => u.UnansweredFriendRequests, sender);
                    tasks.Add(_database.FriendList.UpdateOneAsync(u => u.Username == receiver, updateReceiver));
                }

                await Task.WhenAll(tasks);
                callback();
            });
        }

        /// <inheritdoc />
        public async void GetFriends(string username, Action<IFriendList> callback)
        {
            //Gets friend list data from the database and builds a User object to send back
            var friendList = await _database.FriendList.Find(u => u.Username == username).FirstOrDefaultAsync();
            if (friendList == null)
            {
                await _database.FriendList.InsertOneAsync(new FriendList(username));
                friendList = new FriendList(username);
            }
            callback(new FriendListDto(friendList));
        }

        #region Helpers

        /// <summary>
        ///     Return the friends list of the player
        /// </summary>
        /// <param name="usernames">The username of the player</param>
        /// <param name="callback">The action executed</param>
        private async void GetFriendLists(IEnumerable<string> usernames, Action<FriendList[]> callback)
        {
            var friendLists = new List<FriendList>();
            var tasks = usernames.Select(username => _database.FriendList.Find(u => u.Username == username).FirstOrDefaultAsync()).ToList();

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                var friendList = await task;
                friendLists.Add(friendList);
            }

            callback(friendLists.ToArray());
        }

        #endregion

        #endregion

        #region Auctions

        /// <inheritdoc />
        public async void AddAuction(AuctionRoom auction, Action callback)
        {
            await _database.AuctionRoom.InsertOneAsync(auction);
            callback();
        }

        /// <inheritdoc />
        public async void GetAuction(uint auctionId, Action<AuctionRoom> callback)
        {
            var auctionRoom = await _database.AuctionRoom.Find(a => a.Id == auctionId).FirstOrDefaultAsync();
            callback(auctionRoom);
        }
        
        /// <inheritdoc />
        public void GetAuctions(Action<List<AuctionRoom>> callback)
        {
            var auctionRoom = _database.AuctionRoom.Find(_ => true).ToList();
            callback(auctionRoom);
        }

        /// <inheritdoc />
        public void RemoveAuction(uint auctionId, Action callback)
        {
            _database.AuctionRoom.DeleteOne(a => a.Id == auctionId);
            callback();
        }

        /// <inheritdoc />
        public async void GetMine(uint auctionId, Action<Mine> callback)
        {
            var mine = await _database.MineData.Find(m => m.Id == auctionId).FirstOrDefaultAsync();
            callback(mine);
        }
        
        /// <inheritdoc />
        public void GetMines(string username, Action<List<Mine>> callback)
        {
            var mines = _database.MineData.Find(m => m.Owner == username).ToList();
            callback(mines);
        }

        /// <inheritdoc />
        public async void AddScan(uint auctionId, MineScan scan, Action callback)
        {
            var filter = Builders<AuctionRoom>.Filter.Eq(u => u.Id, auctionId);
            var update = Builders<AuctionRoom>.Update.AddToSet(a => a.MineScans, scan);
            
            await _database.AuctionRoom.UpdateOneAsync(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public async void AddBid(uint auctionId, Bid bid, Action callback)
        {
            var filter = Builders<AuctionRoom>.Filter.Eq(a => a.Id, auctionId);
            var update = Builders<AuctionRoom>.Update.Set(a => a.LastBid, bid);
            
            await _database.AuctionRoom.UpdateOneAsync(filter, update);
            callback();
        }

        #endregion

        #region Mine

        /// <inheritdoc />
        public async void AddMine(Mine mine, Action callback)
        {
            await _database.MineData.InsertOneAsync(mine);
            callback();
        }
        
        public void SaveMineBlocks(uint mineId, bool[] blocks, Action callback)
        {
            var filter = Builders<Mine>.Filter.Eq(u => u.Id, mineId);
            var update = Builders<Mine>.Update.Set(a => a.Blocks, blocks);
            
            _database.MineData.UpdateOne(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public void RemoveMine(uint mineId, Action callback)
        {
            _database.MineData.DeleteOne(a => a.Id == mineId);
            callback();
        }

        #endregion
    }
}