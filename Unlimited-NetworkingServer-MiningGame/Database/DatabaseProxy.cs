using System;
using DarkRift;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame.Database
{
    public class DatabaseProxy : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;
        
        public IDataLayer DataLayer { get; set; }

        private static readonly object InitializeLock = new object();
        
        public DatabaseProxy(PluginLoadData pluginLoadData) : base(pluginLoadData) {}

        public void SetDatabase(IDataLayer dataLayer)
        {
            lock (InitializeLock)
            {
                if (DataLayer == dataLayer)
                {
                    Logger.Info($"Database: {dataLayer.Name} is already selected");
                }

                if (DataLayer != null)
                {
                    Logger.Warning($"Switching from Database: {DataLayer.Name} to Database: {dataLayer.Name}");
                }
                else
                {
                    Logger.Info($"Selected Database: {dataLayer.Name}");
                }
                
                DataLayer = dataLayer;
            }
        }

        #region ErrorHandling

        public void DatabaseError(IClient client, ushort tag, Exception e)
        {
            Logger.Error("Database error: " + e.Message + " - " + e.StackTrace);

            using (var writer = DarkRiftWriter.Create())
            {
                writer.Write((byte) 2);

                using (var msg = Message.Create(tag, writer))
                {
                    client.SendMessage(msg, SendMode.Reliable);
                }
            }
        }

        #endregion
        
    }
}