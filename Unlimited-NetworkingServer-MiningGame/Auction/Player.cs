using System;
using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Auction
{
    /// <summary>
    ///     Auction player
    /// </summary>
    public class Player : IDarkRiftSerializable
    {
        public string Name { get; }
        public bool IsHost { get; private set; }
        
        public Player(string name, bool isHost)
        {
            Name = name;
            IsHost = isHost;
        }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Name);
            e.Writer.Write(IsHost);
        }
        
        /// <summary>
        ///     Sets the host of the room
        /// </summary>
        /// <param name="isHost">True if the player is the host and false otherwise</param>
        public void SetHost(bool isHost)
        {
            IsHost = isHost;
        }
    }
}