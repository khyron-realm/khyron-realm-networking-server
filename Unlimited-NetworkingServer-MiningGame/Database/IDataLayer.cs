using System;
using MongoDB.Driver;
using Unlimited_NetworkingServer_MiningGame.GameElements;
using Unlimited_NetworkingServer_MiningGame.MongoDbConnector;

namespace Unlimited_NetworkingServer_MiningGame.Database
{
    /// <summary>
    ///     Data layer interface that contains declarations for database operations
    /// </summary>
    public interface IDataLayer
    {
        string Name { get; }

        #region Login

        /// <summary>
        ///     Find a user in the database
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void GetUser(string username, Action<IUser> callback);

        /// <summary>
        ///     Checks if a username is available on the database
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void UsernameAvailable(string username, Action<bool> callback);

        /// <summary>
        ///     Adds a new user in the database
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="password">The user password</param>
        /// <param name="callback">Action executed</param>
        void AddNewUser(string username, string password, Action callback);

        /// <summary>
        ///     Deletes a user from the database
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void DeleteUser(string username, Action callback);

        #endregion

        #region Game

        /// <summary>
        ///     Initializes player data for new users
        /// </summary>
        /// <param name="player">The player data object</param>
        /// <param name="callback">Action executed</param>
        void AddPlayerData(PlayerData player, Action callback);
        
        /// <summary>
        ///     Retrieves player data from the database
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void GetPlayerData(string username, Action<PlayerData> callback);
        
        /// <summary>
        ///     Retrieves player level from the database 
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void GetPlayerLevel(string username, Action<uint> callback);
        
        /// <summary>
        ///     Updates the player level
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="level">The new player level</param>
        /// <param name="callback">Action executed</param>
        void UpdatePlayerLevel(string username, byte level, Action callback);
        
        /// <summary>
        ///     Retrieves player experience from the database 
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void GetPlayerExperience(string username, Action<uint> callback);
        
        /// <summary>
        ///     Updates the player experience
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="experience">The new player experience</param>
        /// <param name="callback">Action executed</param>
        void UpdatePlayerExperience(string username, ushort experience, Action callback);

        /// <summary>
        ///     Retrieves player energy from the database 
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void GetPlayerEnergy(string username, Action<uint> callback);
        
        /// <summary>
        ///     Updates the player energy
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="energy">The new player energy</param>
        /// <param name="callback">Action executed</param>
        void UpdatePlayerEnergy(string username, uint energy, Action callback);

        /// <summary>
        ///     Returns true if 
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="queueNumber">The task number in queue</param>
        /// <param name="callback">Action executed</param>
        void TaskAvailable(string username, byte queueNumber, Action<bool> callback);

        /// <summary>
        ///     Adds a resource conversion task
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="time">The finalization time</param>
        /// <param name="callback">Action executed</param>
        void AddResourceConversion(string username, long time, Action callback);
        
        /// <summary>
        ///     Cancels the resource conversion task
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void CancelResourceConversion(string username, Action callback);

        /// <summary>
        ///     Adds a upgrade task for the robot id and part
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="robotId">The robot type</param>
        /// <param name="robotPart">The robot part</param>
        /// <param name="time">The finalization time</param>
        /// <param name="callback">Action executed</param>
        void AddRobotUpgrade(string username, byte robotId, long time, Action callback);

        /// <summary>
        ///     Cancels the robot upgrade
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void CancelRobotUpgrade(string username, Action callback);

        /// <summary>
        ///     Adds a building task for the robot id
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="queueNumber"></param>
        /// <param name="robotId">The robot type</param>
        /// <param name="time">The finalization time</param>
        /// <param name="callback">Action executed</param>
        void AddRobotBuild(string username, byte queueNumber, byte robotId, long time, Action callback);

        /// <summary>
        ///     Cancels the robot building
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="queueNumber">The robot type</param>
        /// <param name="callback">Action executed</param>
        void CancelRobotBuild(string username, byte queueNumber, Action callback);

        #endregion

        #region Parameters

        /// <summary>
        ///     Store the game parameters
        /// </summary>
        /// <param name="parameters">The game parameters</param>
        /// <param name="callback">Action executed</param>
        void AddGameParameters(GameParameters parameters, Action callback);

        /// <summary>
        ///     Retrieves the game parameters
        /// </summary>
        /// <param name="callback">Action executed</param>
        void GetGameParameters(Action<GameParameters> callback);

        #endregion
    }
}