using System;
using DarkRift;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame.Database
{
    /// <summary>
    ///     Database proxy for setting the database
    /// </summary>
    public class DatabaseProxy : Plugin
    {
        private static readonly object InitializeLock = new object();

        public DatabaseProxy(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        public IDataLayer DataLayer { get; set; }

        /// <summary>
        ///     Sets the database layer
        /// </summary>
        /// <param name="dataLayer"></param>
        public void SetDatabase(IDataLayer dataLayer)
        {
            lock (InitializeLock)
            {
                if (DataLayer == dataLayer) Logger.Info($"Database: {dataLayer.Name} is already selected");

                if (DataLayer != null)
                    Logger.Warning($"Switching from Database: {DataLayer.Name} to Database: {dataLayer.Name}");
                else
                    Logger.Info($"Selected Database: {dataLayer.Name}");

                DataLayer = dataLayer;
            }
        }

        #region ErrorHandling

        /// <summary>
        ///     Sends a database connection error 2 to the client
        /// </summary>
        /// <param name="client">The client where the error occured</param>
        /// <param name="tag">Mesage tag</param>
        /// <param name="e">Returned exception</param>
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