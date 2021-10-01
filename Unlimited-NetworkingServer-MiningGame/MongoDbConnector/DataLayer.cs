using System;
using System.Threading.Tasks;
using DarkRift;
using DarkRift.Server;
using MongoDB.Driver;
using Unlimited_NetworkingServer_MiningGame.Database;

namespace Unlimited_NetworkingServer_MiningGame.MongoDbConnector
{
    internal class DataLayer : IDataLayer
    {
        public string Name { get; }
        private readonly MongoDbConnector _database;

        public DataLayer(string name, MongoDbConnector database)
        {
            Name = name;
            _database = database;
        }

        #region Login

        public async void GetUser(string username, Action<IUser> callback)
        {
            var user = await _database.Users.Find(u => u.Username == username).FirstOrDefaultAsync();
            callback(user);
        }

        public async void UsernameAvailable(string username, Action<bool> callback)
        {
            callback(await _database.Users.Find(u => u.Username == username).FirstOrDefaultAsync() == null);
        }

        public async void AddNewUser(string username, string password, Action callback)
        {
            await _database.Users.InsertOneAsync(new User(username, password));
            callback();
        }

        public async void DeleteUser(string username, Action callback)
        {
            await _database.Users.DeleteOneAsync(u => u.Username == username);
            callback();
        }
        
        #endregion
    }
}