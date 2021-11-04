using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame.Chat
{
    /// <summary>
    ///     A chat group of players
    /// </summary>
    public class ChatGroup : IDarkRiftSerializable
    {
        public string Name;
        public Dictionary<string, IClient> Users = new Dictionary<string, IClient>();

        public ChatGroup(string name)
        {
            Name = name;
        }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Name);
            e.Writer.Write(Users.Keys.ToArray());
        }

        /// <summary>
        ///     Adds a player to the users dictionary
        /// </summary>
        /// <param name="username">The player username</param>
        /// <param name="client">The client connected</param>
        /// <returns>True if the user is added, or false otherwise</returns>
        internal bool AddPlayer(string username, IClient client)
        {
            if (Users.ContainsKey(username)) return false;

            Users[username] = client;
            return true;
        }

        /// <summary>
        ///     Removes a player from the users dictionary
        /// </summary>
        /// <param name="username">The player username</param>
        internal void RemovePlayer(string username)
        {
            Users.Remove(username);
        }
    }
}