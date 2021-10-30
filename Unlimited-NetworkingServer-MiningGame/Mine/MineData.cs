using System.Xml.Schema;
using DarkRift;
using MongoDB.Bson.Serialization.Attributes;
using Unlimited_NetworkingServer_MiningGame.Game;

namespace Unlimited_NetworkingServer_MiningGame.Mine
{
    /// <summary>
    ///     Mine details
    /// </summary>
    public class MineData : IDarkRiftSerializable
    {
        [BsonId]
        public ushort Id { get; set; }
        public ushort Size { get; set; }
        public MineSeed Seed { get; set; }
        public Block[] Scans { get; set; }
        public MiningRobot[] Robots { get; set; }

        public MineData(ushort id, ushort size, MineSeed seed)
        {
            Id = id;
            Size = size;
            Seed = seed;
            Scans = new Block[] { };
            Robots = new MiningRobot[] { };
        }

        public void Deserialize(DeserializeEvent e)
        {
            
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Size);
            e.Writer.Write(Seed);
        }
        
        /// <summary>
        ///     Sets the mine scans
        /// </summary>
        /// <param name="scans">The scans performed by the user</param>
        public void AddScans(Block[] scans)
        {
            Scans = scans;
        }

        /// <summary>
        ///     Sets the mining robots
        /// </summary>
        /// <param name="robots">The robots deployed in the mine</param>
        public void AddRobots(MiningRobot[] robots)
        {
            Robots = robots;
        }
    }
}