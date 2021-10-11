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
        /// 
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void GetPlayerData(string username, Action<PlayerData> callback);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="callback">Action executed</param>
        void GetPlayerEnergy(string username, Action<uint> callback);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username">The user name</param>
        /// <param name="callback">Action executed</param>
        void InitializePlayerData(string username, Action callback);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="level"></param>
        /// <param name="callback">Action executed</param>
        void UpdatePlayerLevel(string username, byte level, Action callback);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="callback">Action executed</param>
        void AddResourceConversion(string username, long time, Action callback);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="callback">Action executed</param>
        void RemoveResourceConversion(string username, Action callback);

        #endregion

        #region Parameters

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="callback">Action executed</param>
        void AddGameParameters(GameParameters parameters, Action callback);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="callback">Action executed</param>
        void GetGameParameters(Action<GameParameters> callback);

        #endregion
    }
}