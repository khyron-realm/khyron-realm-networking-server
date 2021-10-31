using System.Collections.Generic;

namespace Unlimited_NetworkingServer_MiningGame.Database
{
    public interface IFriendList
    {
        List<string> Friends { get; }
        List<string> OpenFriendRequests { get; }
        List<string> UnansweredFriendRequests { get; }
    }
}