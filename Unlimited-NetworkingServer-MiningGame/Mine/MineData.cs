using DarkRift;
using MongoDB.Bson.Serialization.Attributes;
using Unlimited_NetworkingServer_MiningGame.Auction;

namespace Unlimited_NetworkingServer_MiningGame.Mine
{
    /// <summary>
    ///     Mine details
    /// </summary>
    public class MineData : IDarkRiftSerializable
    {
        [BsonId]
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public MineGenerationValues GenerationValues { get; set; }
        public bool[] BlocksValues { get; set; }
        public MineScan[] Scans { get; set; }

        public MineData()
        { }

        public MineData(uint id, string name, string owner)
        {
            Id = id;
            Name = name;
            Owner = owner;
            GenerationValues = new MineGenerationValues();
            BlocksValues = new bool[] { };
            Scans = new MineScan[] { };
        }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(GenerationValues);
            e.Writer.Write(BlocksValues);
            e.Writer.Write(Scans);
        }
    }
}