using System;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame.Mine
{
    public class MinePlugin : Plugin
    {
        public MinePlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;
    }
}