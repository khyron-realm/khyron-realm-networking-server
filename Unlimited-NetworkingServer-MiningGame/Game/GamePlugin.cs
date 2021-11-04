using System;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Database;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    /// <summary>
    ///     Game manager for updating game elements
    /// </summary>
    public class GamePlugin : Plugin
    {
        private DatabaseProxy _database;
        private GameData _data;

        protected override void Loaded(LoadedEventArgs args)
        {
            if (_database == null) _database = PluginManager.GetPluginByType<DatabaseProxy>();
        }
        
        public GamePlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
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
            
            _data = new Game.GameData(version);
            
            Logger.Info("Initialized the game data to default values for the version " + version);
        }
        
        /// <summary>
        ///     Stores the game data in the database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The command event object</param>
        private void StoreGameData(object sender, CommandEventArgs e)
        {
            if(_data != null)
            {
                _database.DataLayer.AddGameData(_data, () => { Logger.Info("Adding game data"); });
            }
            else 
            {
                Logger.Error("Game data is not initialized");
            }
            
        }

        /// <summary>
        ///     Gets the game data in the database
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The command event object</param>
        private void ExtractGameData(object sender, CommandEventArgs e)
        {
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
                        _data = gameData;
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
                        _data = gameData;
                        Logger.Info("Getting game data version " + _data.Version);
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