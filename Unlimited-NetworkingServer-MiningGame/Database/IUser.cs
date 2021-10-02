namespace Unlimited_NetworkingServer_MiningGame.Database
{
    /// <summary>
    ///     User interface for storing data into the database
    /// </summary>
    public interface IUser
    {
        string Username { get; }
        string Password { get; }
    }
}