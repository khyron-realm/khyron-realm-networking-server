using System;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;
using Unlimited_NetworkingServer_MiningGame.GameElements;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    /// <summary>
    ///     Game manager for updating game elements
    /// </summary>
    public class GameManager : Plugin
    {
        private static readonly object InitializeLock = new object();
        private DatabaseProxy _database;
        private GameParameters _parameters;

        public GameManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        public override Command[] Commands => new[]
        {
            new Command("DefaultParameters", "Store the default game parameters in the database", "DefaultParameters",
                DefaultParameters),
            new Command("StoreParameters", "Store the game parameters in the database", "StoreParameters",
                StoreParameters),
            new Command("GetParameters", "Get the game parameters from the database", "GetParameters",
                GetGameParameters)
        };

        /// <summary>
        ///     Initialize the database object with the DB plugin
        /// </summary>
        private void InitializeDb()
        {
            if (_database == null)
                lock (InitializeLock)
                {
                    if (_database == null)
                        _database = PluginManager.GetPluginByType<DatabaseProxy>();
                }
        }

        #region Commands
        
        /// <summary>
        ///     Initializes the default game parameters
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The command event object</param>
        private void DefaultParameters(object sender, CommandEventArgs e)
        {
            Resource[] resources =
            {
                new Resource(0, "Silicon", 16, 0),
                new Resource(1, "Lithium", 32, 0),
                new Resource(2, "Titanium", 64, 0)
            };

            Robot[] robots =
            {
                new Robot(0, "Worker", 0, 0, 0, 0),
                new Robot(1, "Probe", 0, 0, 0, 0),
                new Robot(2, "Crusher", 0, 0, 0, 0)
            };

            _parameters = new GameParameters(
                1,
                3,
                3,
                15,
                30,
                10,
                60000,
                100000,
                100000,
                resources, robots);
            
            Logger.Info("Initialized the parameters to default values");
        }
        
        /// <summary>
        ///     Stores the game parameters in the database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The command event object</param>
        private void StoreParameters(object sender, CommandEventArgs e)
        {
            InitializeDb();

            if(_parameters != null)
            {
                _database.DataLayer.AddGameParameters(_parameters, () => { Logger.Info("Adding game parameters"); });
            }
            else 
            {
                Logger.Error("Parameters are not initialized");
            }
            
        }

        /// <summary>
        ///     Stores the game parameters in the database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The command event object</param>
        private void GetGameParameters(object sender, CommandEventArgs e)
        {
            InitializeDb();

            try
            {
                _database.DataLayer.GetGameParameters(_parameters =>
                {
                    Logger.Info("Getting game parameters version " + _parameters.Version);
                });
            }
            catch (NullReferenceException)
            {
                Logger.Error("No parameters stored");
            }
        }

        #endregion
    }
}