using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Unlimited_NetworkingServer_MiningGame.Friends
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