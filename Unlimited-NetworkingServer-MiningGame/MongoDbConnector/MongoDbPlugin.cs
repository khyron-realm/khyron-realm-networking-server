using System;
using System.IO;
using System.Xml.Linq;
using DarkRift.Server;
using MongoDB.Driver;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Headquarters;

namespace Unlimited_NetworkingServer_MiningGame.MongoDbConnector
{
    /// <summary>
    ///     MongoDB connection that sets up the MongoDB connection
    /// </summary>
    public class MongoDbPlugin : Plugin
    {
        private const string ConfigPath = @"Plugins/MongoDbConnector.xml";
        private static readonly object InitializeLock = new object();
        private readonly IDataLayer _dataLayer;
        private readonly IMongoDatabase _mongoDatabase;
        private DatabaseProxy _database;

        public MongoDbPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            LoadConfig(out var connectionString, out var database);

            try
            {
                var client = new MongoClient(connectionString);
                _mongoDatabase = client.GetDatabase(database);
                GetCollections();
            }
            catch (Exception ex)
            {
                Logger.Fatal("Failed to connect to MongoDB: " + ex.Message + " - " + ex.StackTrace);
            }

            _dataLayer = new DataLayer("MongoDB", this);

            ClientManager.ClientConnected += OnPlayerConnected;
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        public override Command[] Commands => new[]
        {
            new Command("LoadMongo", "Loads MongoDB Database", "", LoadDbCommand)
        };

        public IMongoCollection<User> Users { get; private set; }
        public IMongoCollection<PlayerData> PlayerData { get; private set; }
        public IMongoCollection<Game.GameData> GameData { get; private set; }

        /// <summary>
        ///     Create or load the config document for setting the database connection
        /// </summary>
        /// <param name="connectionString">The connection string for accessing the database</param>
        /// <param name="database">The database name</param>
        private void LoadConfig(out string connectionString, out string database)
        {
            XDocument document;

            if (!File.Exists(ConfigPath))
            {
                document = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XComment("Enter your connection data here: "),
                    new XElement("MongoDB",
                        new XAttribute("ConnectionString", "mongodb://localhost:27017"),
                        new XAttribute("Database", "test"))
                );

                try
                {
                    document.Save(ConfigPath);
                    Logger.Info(
                        "Created /Plugins/MongoDbConnector.xml. Adjust the connection string and restart the server");
                    connectionString = "mongodb://localhost:27017";
                    database = "test";
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to create DbConnector.xml: " + ex.Message + " - " + ex.StackTrace);
                    connectionString = null;
                    database = null;
                    return;
                }
            }

            try
            {
                document = XDocument.Load(ConfigPath);

                connectionString = document.Element("MongoDB").Attribute("ConnectionString").Value;
                database = document.Element("MongoDB").Attribute("Database").Value;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load MongoDbConnector.xml: " + ex.Message + " - " + ex.StackTrace);
                connectionString = null;
                database = null;
            }
        }

        /// <summary>
        ///     Handler for player connection that initializes the database connection
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            if (_database == null)
                lock (InitializeLock)
                {
                    if (_database == null)
                    {
                        _database = PluginManager.GetPluginByType<DatabaseProxy>();
                        _database.SetDatabase(_dataLayer);
                    }
                }
        }

        /// <summary>
        ///     Initializes the database collections
        /// </summary>
        private void GetCollections()
        {
            Users = _mongoDatabase.GetCollection<User>("Users");
            PlayerData = _mongoDatabase.GetCollection<PlayerData>("PlayerData");
            GameData = _mongoDatabase.GetCollection<Game.GameData>("GameData");
        }

        #region Commands

        /// <summary>
        ///     Command for loading the MongoDB database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The client object</param>
        public void LoadDbCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
                lock (InitializeLock)
                {
                    if (_database == null) _database = PluginManager.GetPluginByType<DatabaseProxy>();
                }

            _database.SetDatabase(_dataLayer);
        }

        #endregion
    }
}