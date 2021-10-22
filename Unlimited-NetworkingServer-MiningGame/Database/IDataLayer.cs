using System;
using Unlimited_NetworkingServer_MiningGame.Game;
using Unlimited_NetworkingServer_MiningGame.Headquarters;

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
        void SetPlayerLevel(string username, byte level, Action callback);
        
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
        void SetPlayerExperience(string username, ushort experience, Action callback);

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
        void SetPlayerEnergy(string username, uint energy, Action callback);
        
        /// <summary>
        ///     Retrieves player resources from the database 
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void GetPlayerResources(string username, Action<Resource[]> callback);

        /// <summary>
        ///     Updates the player resources
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="resources">The new player resources</param>
        /// <param name="callback">Action executed</param>
        void SetPlayerResources(string username, Resource[] resources, Action callback);
        
        /// <summary>
        ///     Retrieves player robots from the database 
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void GetPlayerRobots(string username, Action<Robot[]> callback);

        /// <summary>
        ///     Updates a single player robot
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="robotId">The new player robot id</param>
        /// <param name="robot">The new player robot</param>
        /// <param name="callback">Action executed</param>
        void SetPlayerRobot(string username, byte robotId, Robot robot, Action callback);
        
        /// <summary>
        ///     Updates the player robots
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="robots">The new player robots</param>
        /// <param name="callback">Action executed</param>
        void SetPlayerRobots(string username, Robot[] robots, Action callback);
        
        /// <summary>
        ///     Returns true if 
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="id">The task number in queue</param>
        /// <param name="type">The robot type</param>
        /// <param name="callback">Action executed</param>
        void TaskAvailable(string username, ushort id, byte type,  Action<bool> callback);

        /// <summary>
        ///     Adds a task
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="id">The number in the queue</param>
        /// <param name="type">The task type</param>
        /// <param name="element">The selected element</param>
        /// <param name="time">The start time</param>
        /// <param name="callback">Action executed</param>
        void AddTask(string username, ushort id, byte type, byte element, long time, Action callback);

        /// <summary>
        ///     Finish a task
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="id">The task id</param>
        /// <param name="type">The task type</param>
        /// <param name="callback">Action executed</param>
        void FinishTask(string username, ushort id, byte type, Action callback);

        /// <summary>
        ///     Updated following tasks
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="id">The number in the queue</param>
        /// <param name="type">The task type</param>
        /// <param name="time">The start time</param>
        /// <param name="callback">Action executed</param>
        void UpdateNextTask(string username, ushort id, byte type, long time, Action callback);

        #endregion

        #region GameData

        /// <summary>
        ///     Store the game data
        /// </summary>
        /// <param name="data">The game parameters</param>
        /// <param name="callback">Action executed</param>
        void AddGameData(GameData data, Action callback);

        /// <summary>
        ///     Retrieves the game data
        /// </summary>
        /// <param name="callback">Action executed</param>
        void GetGameData(Action<GameData> callback);
        
        /// <summary>
        ///     Retrieves the game data
        /// </summary>
        /// <param name="version">The game data version</param>
        /// <param name="callback">Action executed</param>
        void GetGameData(ushort version, Action<GameData> callback);

        #endregion
    }
}