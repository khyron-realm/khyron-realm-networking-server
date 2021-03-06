using System;
using System.IO;
using System.Xml.Linq;
using DarkRift.Server;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using Unlimited_NetworkingServer_MiningGame.Auction;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.Friends;
using Unlimited_NetworkingServer_MiningGame.Game;
using Unlimited_NetworkingServer_MiningGame.Headquarters;
using Unlimited_NetworkingServer_MiningGame.Login;
using Unlimited_NetworkingServer_MiningGame.Mines;

namespace Unlimited_NetworkingServer_MiningGame.MongoDbConnector
{
    /// <summary>
    ///     MongoDB connection that sets up the MongoDB connection
    /// </summary>
    public class MongoDbPlugin : Plugin
    {
        private const string ConfigPath = @"Plugins/MongoDbConnector.xml";
        private readonly IDataLayer _dataLayer;
        private readonly IMongoDatabase _mongoDatabase;
        private DatabaseProxy _database;
        private static byte _initialCheck = 0;
        
        public static bool IsConnected;

        public MongoDbPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
            LoadConfig(out var connectionString, out var database);

            try
            {
                var client = new MongoClient(connectionString);
                client.Cluster.DescriptionChanged += Cluster_DescriptionChanged;
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
        protected override void Loaded(LoadedEventArgs args)
        {
            if (_database == null)
            {
                _database = PluginManager.GetPluginByType<DatabaseProxy>();
                _database.SetDatabase(_dataLayer);
            }

            Logger.Info("Loaded Database: MongoDB");
        }
        
        public void Cluster_DescriptionChanged(object sender, ClusterDescriptionChangedEventArgs e)
        {
            switch (e.NewClusterDescription.State)
            {
                case ClusterState.Disconnected:
                    if(_initialCheck > 1)
                    {
                        Logger.Fatal("Failed to connect to MongoDB database");
                        Logger.Fatal("Shutting down the server");
                        _initialCheck = 0;
                        Environment.Exit(0);
                    }
                    _initialCheck++;
                    IsConnected = false;
                    break;
                case ClusterState.Connected:
                    IsConnected = true;
                    break;
            }
        }

        public IMongoCollection<User> Users { get; private set; }
        public IMongoCollection<PlayerData> PlayerData { get; private set; }
        public IMongoCollection<FriendList> FriendList { get; private set; }
        public IMongoCollection<GameData> GameData { get; private set; }
        public IMongoCollection<AuctionRoom> AuctionRoom { get; private set; }
        public IMongoCollection<Mine> MineData { get; private set; }

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
        { }

        /// <summary>
        ///     Initializes the database collections
        /// </summary>
        private void GetCollections()
        {
            Users = _mongoDatabase.GetCollection<User>("Users");
            PlayerData = _mongoDatabase.GetCollection<PlayerData>("PlayerData");
            FriendList = _mongoDatabase.GetCollection<FriendList>("FriendList");
            GameData = _mongoDatabase.GetCollection<GameData>("GameData");
            AuctionRoom = _mongoDatabase.GetCollection<AuctionRoom>("AuctionRoom");
            MineData = _mongoDatabase.GetCollection<Mine>("MineData");
        }
    }
}