using System;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame.Friends
{
    /// <summary>
    ///     Friends manager that handles friends and requests
    /// </summary>
    public class FriendsPlugin : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        public FriendsPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
        }
        
        
    }
}