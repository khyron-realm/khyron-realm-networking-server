using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.GameData
{
    /// <summary>
    ///     Stores the resource data
    /// </summary>
    public class RobotDetails : IDarkRiftSerializable
    {
        public RobotDetails(byte id, string name, byte health, byte movementSpeed, byte miningSpeed, byte buildTime, byte upgradeTime, byte price, byte maxCount)
        {
            Id = id;
            Name = name;
            Health = health;
            MovementSpeed = movementSpeed;
            MiningSpeed = miningSpeed;
            BuildTime = buildTime;
            UpgradeTime = upgradeTime;
            Price = price;
            MaxCount = maxCount;
        }

        public byte Id { get; set; }
        public string Name { get; set; }
        public byte Health { get; set; }
        public byte MovementSpeed { get; set; }
        public byte MiningSpeed { get; set; }
        public byte BuildTime { get; set; }
        public byte UpgradeTime { get; set; }
        public byte Price { get; set; }
        public byte MaxCount { get; set; }

        
        

        /// <summary>
        ///     Deserialization method for robot data
        /// </summary>
        /// <param name="e">Deserialize event</param>
        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadByte();
            Name = e.Reader.ReadString();
            Health = e.Reader.ReadByte();
            MovementSpeed = e.Reader.ReadByte();
            MiningSpeed = e.Reader.ReadByte();
            BuildTime = e.Reader.ReadByte();
            UpgradeTime = e.Reader.ReadByte();
            Price = e.Reader.ReadByte();
            MaxCount = e.Reader.ReadByte();
        }

        /// <summary>
        ///     Serialization method for robot data
        /// </summary>
        /// <param name="e">Serialize event</param>
        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(Health);
            e.Writer.Write(MovementSpeed);
            e.Writer.Write(MiningSpeed);
            e.Writer.Write(BuildTime);
            e.Writer.Write(UpgradeTime);
            e.Writer.Write(Price);
            e.Writer.Write(MaxCount);
        }
    }
}