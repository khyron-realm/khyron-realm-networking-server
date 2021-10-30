using System.Collections.Generic;

namespace Unlimited_NetworkingServer_MiningGame.Friends
{
    public class FriendListDto
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