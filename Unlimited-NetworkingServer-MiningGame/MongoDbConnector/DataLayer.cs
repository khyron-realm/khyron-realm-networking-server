using System;
using MongoDB.Driver;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Headquarters;

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

        #region Game
        
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
        public async void GetPlayerExperience(string username, Action<uint> callback)
        {
            var experience = await _database.PlayerData.Find(u => u.Id == username).Project(u => u.Experience)
                .FirstOrDefaultAsync();
            callback(experience);
        }

        /// <inheritdoc />
        public async void SetPlayerExperience(string username, ushort experience, Action callback)
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
        public async void TaskAvailable(string username, ushort id, byte type, Action<bool> callback)
        {
            var filter = Builders<PlayerData>.Filter.And(
                Builders<PlayerData>.Filter.Eq(u => u.Id, username),
                Builders<PlayerData>.Filter.ElemMatch(u => u.TaskQueue, Builders<BuildTask>.Filter.And(
                        Builders<BuildTask>.Filter.Eq(b => b.Id, id),
                        Builders<BuildTask>.Filter.Eq(b => b.Type, type)))
            );
            callback(await _database.PlayerData.Find(filter).FirstOrDefaultAsync() == null);
        }
        
        /// <inheritdoc />
        public async void AddTask(string username, ushort id, byte type, byte element, long time, Action callback)
        {
            var upgrade = new BuildTask(id, type, element, time);
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.AddToSet(u => u.TaskQueue, upgrade);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void FinishTask(string username, ushort id, byte type, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.PullFilter( u => u.TaskQueue, 
                Builders<BuildTask>.Filter.And(Builders<BuildTask>.Filter.Lte(b => b.Id, id),
                    Builders<BuildTask>.Filter.Eq(b => b.Type, type)
                ));
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public async void UpdateNextTask(string username, ushort id, byte type, long time, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.And(
                Builders<PlayerData>.Filter.Eq(u => u.Id, username),
                Builders<PlayerData>.Filter.ElemMatch(u => u.TaskQueue, Builders<BuildTask>.Filter.And(
                    Builders<BuildTask>.Filter.Gt(b => b.Id, id),
                    Builders<BuildTask>.Filter.Eq(b => b.Type, type)))
                );
            var update = Builders<PlayerData>.Update.Set(b => b.TaskQueue[-1].StartTime, time);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        #endregion

        #region Parameters

        /// <inheritdoc />
        public async void AddGameData(Game.GameData data, Action callback)
        {
            await _database.GameData.InsertOneAsync(data);
            callback();
        }
        
        /// <inheritdoc />
        public async void GetGameData(Action<Game.GameData> callback)
        {
            var filter = Builders<Game.GameData>.Filter.Empty;
            var sort = Builders<Game.GameData>.Sort.Descending(p => p.Version);
            var gameData = await _database.GameData.Find(filter).Sort(sort).FirstOrDefaultAsync();
            callback(gameData);
        }

        /// <inheritdoc />
        public async void GetGameData(ushort version, Action<Game.GameData> callback)
        {
            var gameData = await _database.GameData.Find(p => p.Version == version).FirstOrDefaultAsync();
            callback(gameData);
        }

        #endregion
    }
}