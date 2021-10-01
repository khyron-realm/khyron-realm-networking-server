using MongoDB.Bson.Serialization.Attributes;
using Unlimited_NetworkingServer_MiningGame.Database;

namespace Unlimited_NetworkingServer_MiningGame.MongoDbConnector
{
    public class User : IUser
    {
        public User(string username, string password)
        {
            Username = username;
            Password = password;
        }

        [BsonId] public string Username { get; }

        public string Password { get; }
    }
}