using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Unlimited_NetworkingServer_MiningGame.Headquarters;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    public class FriendList
    {
        [BsonId]
        public string Username { get; }
        public List<string> Friends;
        public List<string> OpenFriendRequests;
        public List<string> UnansweredFriendRequests;

        public FriendList(string username)
        {
            Username = username;
            Friends = new List<string>();
            OpenFriendRequests = new List<string>();
            UnansweredFriendRequests = new List<string>();
        }
    }
}