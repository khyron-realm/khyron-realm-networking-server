using System;
using System.Linq;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Game;
using Unlimited_NetworkingServer_MiningGame.GameElements;

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
        public async void GetPlayerData(string username, Action<PlayerData> callback)
        {
            PlayerData playerData = await _database.PlayerData.Find(u => u.Id == username).FirstOrDefaultAsync();
            callback(playerData);
        }

        /// <inheritdoc />
        public async void GetPlayerEnergy(string username, Action<uint> callback)
        {
            var energy = await _database.PlayerData.Find(u => u.Id == username).Project(u => u.Energy).FirstOrDefaultAsync();
            callback(energy);
        }

        /// <inheritdoc />
        public async void InitializePlayerData(string username, Action callback)
        {
            // Get game elements
            byte nrRobots = 3;
            byte nrResources = 3;
            string[] resourceNames = {"Silicon", "Lithium", "Titanium"};
            string[] robotNames = {"Worker", "Probe", "Crusher"};
            
            // Create player data
            string id = username;
            byte level = 1;
            ushort experience = 1;
            uint energy = 1000;

            // Create resources
            Resource[] resources = new Resource[nrResources];
            foreach (int iterator in Enumerable.Range(0, nrResources))
            {
                resources[iterator] = new Resource((byte)iterator, resourceNames[iterator], 10, 10);
            }

            // Create robots
            Robot[] robots = new Robot[nrRobots];
            foreach (int iterator in Enumerable.Range(0, nrRobots))
            {
                robots[iterator] = new Robot((byte)iterator, robotNames[iterator], level, 1, 1, 1, 1);
            }
            
            // Create resource conversion task
            var time = DateTime.Now.ToBinary();
            BuildTask resourceConversion = new BuildTask();
            
            // Create robot upgrading tasks
            time = DateTime.Now.ToBinary();
            BuildTask robotUpgrading = new BuildTask();

            // Create robot building tasks
            time = DateTime.Now.ToBinary();
            BuildTask[] robotBuilding = new BuildTask[1];
            robotBuilding[0] = new BuildTask();

            // Create player object
            var newPlayerData = new PlayerData(id, level, experience, energy, resources, robots, resourceConversion, robotUpgrading, robotBuilding);
            
            await _database.PlayerData.InsertOneAsync(newPlayerData);
            callback();
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
        public async void AddResourceConversion(string username, long time, Action callback)
        {
            var conversion = new BuildTask(1, 0, 0, time);
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.ResourceConversion, conversion);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void CancelResourceConversion(string username, Action callback)
        {
            var conversion = new BuildTask();
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.ResourceConversion, conversion);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public async void AddRobotUpgrade(string username, byte robotId, byte robotPart, long time, Action callback)
        {
            var upgrade = new BuildTask(1, robotId, robotPart, time);
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.RobotUpgrade, upgrade);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }
        
        /// <inheritdoc />
        public async void CancelRobotUpgrade(string username, Action callback)
        {
            var upgrade = new BuildTask();
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Set(u => u.RobotUpgrade, upgrade);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void AddRobotBuild(string username, byte queueNumber, byte robotId, long time, Action callback)
        {
            var build = new BuildTask(queueNumber, robotId, 0, time);
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.Push(u => u.RobotBuilding, build);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void CancelRobotBuild(string username, byte queueNumber, Action callback)
        {
            var filter = Builders<PlayerData>.Filter.Eq(u => u.Id, username);
            var update = Builders<PlayerData>.Update.PullFilter(u => u.RobotBuilding, f => f.Id == queueNumber);
            await _database.PlayerData.UpdateOneAsync(filter, update);
            callback();
        }

        /// <inheritdoc />
        public async void AddGameParameters(GameParameters parameters, Action callback)
        {
            await _database.Parameters.InsertOneAsync(parameters);
            callback();
        }

        /// <inheritdoc />
        public async void GetGameParameters(Action<GameParameters> callback)
        {
            var parameters = await _database.Parameters.Find(p => p.Version > 0).FirstOrDefaultAsync();
            callback(parameters);
        }

        #endregion
    }
}