using System;
using MongoDB.Driver;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.GameData;
using Unlimited_NetworkingServer_MiningGame.Headquarters;

namespace Unlimited_NetworkingServer_MiningGame.MongoDbConnector
{
    /// <summary>
    ///     Data layer class that contains implementations for database operations
    /// </summary>
    internal class DataLayer : IDataLayer
    {
        private readonly MongoDbConnector _database;

        public DataLayer(string name, MongoDbConnector database)
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
        public async void UpdatePlayerLevel(string username, byte level, Action callback)
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
        public async void UpdatePlayerExperience(string username, ushort experience, Action callback)
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
        public async void UpdatePlayerEnergy(string username, uint energy, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.Energy, energy);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void TaskAvailable(string username, byte queueNumber, byte type, Action<bool> callback)
        {
            var filter = Builders<PlayerData>.Filter.And(
                Builders<PlayerData>.Filter.Eq(u => u.Id, username),
                Builders<PlayerData>.Filter.ElemMatch(u => u.TaskQueue, Builders<BuildTask>.Filter.And(
                        Builders<BuildTask>.Filter.Eq(b => b.Id, queueNumber),
                        Builders<BuildTask>.Filter.Eq(b => b.Type, type)))
            );
            callback(await _database.PlayerData.Find(filter).FirstOrDefaultAsync() == null);
        }

        /// <inheritdoc />
        public async void AddResourceConversion(string username, byte queueNumber, long time, Action callback)
        {
            var conversion = new BuildTask(queueNumber, GameConstants.ConversionTask, 0, time);
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Push(u => u.TaskQueue, conversion);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void FinishResourceConversion(string username, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.PullFilter(u => u.TaskQueue, f => f.Type == GameConstants.ConversionTask);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public async void AddRobotUpgrade(string username, byte queueNumber, byte robotId, long time, Action callback)
        {
            var upgrade = new BuildTask(queueNumber, GameConstants.UpgradeTask, robotId, time);
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.AddToSet(u => u.TaskQueue, upgrade);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public async void FinishRobotUpgrade(string username, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.PullFilter(u => u.TaskQueue, f => f.Type == GameConstants.UpgradeTask);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void AddRobotBuild(string username, byte queueNumber, byte robotId, long time, Action callback)
        {
            var build = new BuildTask(queueNumber, GameConstants.BuildTask, robotId, time);
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Push(u => u.TaskQueue, build);

            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void FinishRobotBuild(string username, byte queueNumber, Action callback)
        {
            /*
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.PullFilter(u => u.TaskQueue, f => f.Id == queueNumber);
            await _database.PlayerData.UpdateOneAsync(filter, update);       
            callback();
            */
        }
        
        /// <inheritdoc />
        public async void CancelRobotBuild(string username, byte queueNumber, Action callback)
        {
            /*
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.PullFilter(u => u.TaskQueue, f => f.Id == queueNumber);
            await _database.PlayerData.UpdateOneAsync(filter, update);       
            callback();
            */
        }

        #endregion

        #region Parameters

        /// <inheritdoc />
        public async void AddGameParameters(GameParameters parameters, Action callback)
        {
            await _database.Parameters.InsertOneAsync(parameters);
            callback();
        }
        
        /// <inheritdoc />
        public async void GetGameParameters(Action<GameParameters> callback)
        {
            var filter = Builders<GameParameters>.Filter.Empty;
            var sort = Builders<GameParameters>.Sort.Descending(p => p.Version);
            var parameters = await _database.Parameters.Find(filter).Sort(sort).FirstOrDefaultAsync();
            callback(parameters);
        }

        /// <inheritdoc />
        public async void GetGameParameters(ushort version, Action<GameParameters> callback)
        {
            var parameters = await _database.Parameters.Find(p => p.Version == version).FirstOrDefaultAsync();
            callback(parameters);
        }

        #endregion
    }
}