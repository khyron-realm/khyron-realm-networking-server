using System;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    /// <summary>
    ///     Game manager for updating game elements
    /// </summary>
    public class GameManager : Plugin
    {
        private static readonly object InitializeLock = new object();
        private DatabaseProxy _database;
        private GameData Data;

        public GameManager(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
        }

        public override Version Version => new Version(1, 0, 0);
        public override bool ThreadSafe => true;

        public override Command[] Commands => new[]
        {
            new Command("DefaultGameData", "Initialize the game data with the default parameters", "DefaultGameData [version]",
                DefaultGameData),
            new Command("StoreGameData", "Store the game data parameters in the database", "StoreGameData",
                StoreGameData),
            new Command("GetGameData", "Get the game data parameters from the database for the specified version", "GetGameData [version]",
                ExtractGameData)
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
        ///     Initializes the default game data
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The command event object</param>
        private void DefaultGameData(object sender, CommandEventArgs e)
        {
            if (e.Arguments.Length != 1)
            {
                Logger.Warning("Invalid arguments. Enter DefaultGameData [version]");
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

            Data = new Game.GameData(version, 30, 10, 100000, 60000, 
                30, 5, resources, robots, formulas);
            
            Logger.Info("Initialized the game data to default values for the version " + version);
        }
        
        /// <summary>
        ///     Stores the game data in the database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The command event object</param>
        private void StoreGameData(object sender, CommandEventArgs e)
        {
            InitializeDb();

            if(Data != null)
            {
                _database.DataLayer.AddGameData(Data, () => { Logger.Info("Adding game data"); });
            }
            else 
            {
                Logger.Error("Game data data are not initialized");
            }
            
        }

        /// <summary>
        ///     Gets the game data in the database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The command event object</param>
        private void ExtractGameData(object sender, CommandEventArgs e)
        {
            InitializeDb();

            if (e.Arguments.Length > 1)
            {
                Logger.Warning("Invalid arguments. Enter GetGameData [version]");
                return;
            }

            try
            {
                var version = Convert.ToUInt16(e.Arguments[0]);
                try
                {
                    _database.DataLayer.GetGameData(version, gameData =>
                    {
                        Data = gameData;
                        Logger.Info("Getting game data version " + gameData.Version);
                    });
                }
                catch (NullReferenceException)
                {
                    Logger.Error("No parameters stored in the game data");
                }
            }
            catch (IndexOutOfRangeException)
            {
                try
                {
                    _database.DataLayer.GetGameData(gameData =>
                    {
                        Data = gameData;
                        Logger.Info("Getting game data version " + Data.Version);
                    });
                }
                catch (NullReferenceException)
                {
                    Logger.Error("No parameters stored in the game data");
                }
            }
        }
        
        #endregion
    }
}