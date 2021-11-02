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

        internal bool AddPlayer(string username, IClient client)
        {
            if (Users.ContainsKey(username)) return false;

            Users[username] = client;
            return true;
        }

        internal void RemovePlayer(string username)
        {
            Users.Remove(username);
        }
    }
}