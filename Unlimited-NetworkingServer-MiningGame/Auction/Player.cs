using System;
using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Auction
{
    /// <summary>
    ///     Auction player
    /// </summary>
    public class Player : IDarkRiftSerializable
    {
        public ushort Id { get; }
        public string Name { get; }
        public bool IsHost { get; private set; }
        
        public Player(ushort id, string name, bool isHost)
        {
            Id = id;
            IsHost = isHost;
            Name = name;
        }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
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