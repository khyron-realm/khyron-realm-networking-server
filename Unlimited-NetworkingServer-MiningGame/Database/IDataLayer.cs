using System;

namespace Unlimited_NetworkingServer_MiningGame.Database
{
    public interface IDataLayer
    {
        string Name { get; }

        #region Login

        void GetUser(string username, Action<IUser> callback);
        void UsernameAvailable(string username, Action<bool> callback);
        void AddNewUser(string username, string password, Action callback);
        void DeleteUser(string username, Action callback);

        #endregion
    }
}