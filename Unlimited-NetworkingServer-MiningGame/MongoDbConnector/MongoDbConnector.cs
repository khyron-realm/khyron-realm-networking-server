using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using DarkRift;
using DarkRift.Server;
using MongoDB.Driver;
using Unlimited_NetworkingServer_MiningGame.Database;

namespace Unlimited_NetworkingServer_MiningGame.MongoDbConnector
{
    public class MongoDbConnector : Plugin
    {
        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => false;

        public override Command[] Commands => new Command[]
        {
            new Command("LoadMongo", "Loads MongoDB Database", "", LoadDbCommand)
        };

        public IMongoCollection<User> Users { get; private set; }

        private const string ConfigPath = @"Plugins/MongoDbConnector.xml";
        private static readonly object InitializeLock = new object();
        private readonly IDataLayer _dataLayer;
        private readonly IMongoDatabase _mongoDatabase;
        private DatabaseProxy _database;

        public MongoDbConnector(PluginLoadData pluginLoadData) : base(pluginLoadData)
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
                    Logger.Info("Created /Plugins/MongoDbConnector.xml. Adjust the connection string and restart the server");
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

        private void OnPlayerConnected(object sender, ClientConnectedEventArgs e)
        {
            if (_database == null)
            {
                lock (InitializeLock)
                {
                    if (_database == null)
                    {
                        _database = PluginManager.GetPluginByType<DatabaseProxy>();
                        _database.SetDatabase(_dataLayer);
                    }
                }
            }
        }

        private void GetCollections()
        {
            Users = _mongoDatabase.GetCollection<User>("users");
        }

        #region Commands

        public void LoadDbCommand(object sender, CommandEventArgs e)
        {
            if (_database == null)
            {
                lock (InitializeLock)
                {
                    if (_database == null)
                    {
                        _database = PluginManager.GetPluginByType<DatabaseProxy>();
                    }
                }
            }
            
            _database.SetDatabase(_dataLayer);
        }

        #endregion
    }
}