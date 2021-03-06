using MongoDB.Bson.Serialization.Attributes;
using Unlimited_NetworkingServer_MiningGame.Database;

namespace Unlimited_NetworkingServer_MiningGame.Login
{
    /// <summary>
    ///     User class for storing data into the database
    /// </summary>
    public class User : IUser
    {
        [BsonId]
        public string Username { get; }
        public string Password { get; }
        
        public User(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}