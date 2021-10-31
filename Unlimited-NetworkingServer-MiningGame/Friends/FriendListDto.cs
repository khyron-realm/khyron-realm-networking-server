using System.Collections.Generic;
using Unlimited_NetworkingServer_MiningGame.Database;

namespace Unlimited_NetworkingServer_MiningGame.Friends
{
    public class FriendListDto : IFriendList
    {
        public List<string> Friends { get; }
        public List<string> OpenFriendRequests { get; }
        public List<string> UnansweredFriendRequests { get; }

        public FriendListDto(FriendList friends)
        {
            Friends = friends.Friends;
            OpenFriendRequests = friends.OpenFriendRequests;
            UnansweredFriendRequests = friends.UnansweredFriendRequests;
        }
    }
}