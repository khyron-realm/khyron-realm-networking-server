using System;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;

namespace Unlimited_NetworkingServer_MiningGame.GameData
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
            new Command("DefaultParameters", "Store the default game parameters in the database", "DefaultParameters [version]",
                DefaultParameters),
            new Command("StoreParameters", "Store the game parameters in the database", "StoreParameters",
                StoreParameters),
            new Command("GetParameters", "Get the game parameters from the database for the specified version", "GetParameters [version]",
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
            if (e.Arguments.Length != 1)
            {
                Logger.Warning("Invalid arguments. Enter DefaultParameters [version]");
                return;
            }

            var version = Convert.ToUInt16(e.Arguments[0]);
            
            ResourceDetails[] resources =
            {
                new ResourceDetails(0, "Silicon", 400, 10000),
                new ResourceDetails(1, "Lithium", 200, 10000),
                new ResourceDetails(2, "Titanium", 100, 10000)
            };

            RobotDetails[] robots =
            {
                new RobotDetails(0, "Worker", 1000, 10, 20, 5, 5, 100, 1000, 1),
                new RobotDetails(1, "Probe", 1000, 0, 20, 10, 8, 150, 1500, 2),
                new RobotDetails(2, "Crusher", 2000, 5, 40, 30, 15, 300, 3000, 4)
            };

            LevelFormulas[] formulas =
            {
                new LevelFormulas(0, 1, 1, 1)
            };

            _parameters = new GameParameters(version, 30, 10, 100000, 60000, 
                30, 5, resources, robots, formulas);
            
            Logger.Info("Initialized the parameters to default values for the version " + version);
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

            if (e.Arguments.Length > 1)
            {
                Logger.Warning("Invalid arguments. Enter GetParameters [version]");
                return;
            }

            try
            {
                var version = Convert.ToUInt16(e.Arguments[0]);
                try
                {
                    _database.DataLayer.GetGameParameters(version,
                        _parameters => { Logger.Info("Getting game parameters version " + _parameters.Version); });
                }
                catch (NullReferenceException)
                {
                    Logger.Error("No parameters stored");
                }
            }
            catch (IndexOutOfRangeException)
            {
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
        }

        #endregion
    }
}