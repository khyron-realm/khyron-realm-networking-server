using System;
using DarkRift;
using MongoDB.Bson.Serialization.Attributes;

namespace Unlimited_NetworkingServer_MiningGame.Mines
{
    /// <summary>
    ///     Mine details
    /// </summary>
    public class Mine : IDarkRiftSerializable
    {
        [BsonId]
        public uint Id { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public MineGenerator Generator { get; set; }
        public bool[] Blocks { get; set; }
        public MineScan[] Scans { get; set; }
        public byte MapPosition { get; set; }

        public Mine()
        { }

        public Mine(uint id, string name, string owner)
        {
            Id = id;
            Name = name;
            Owner = owner;
            Generator = new MineGenerator();
            Blocks = new bool[] { };
            Scans = new MineScan[] { };
            MapPosition = Byte.MaxValue;
        }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(Generator);
            e.Writer.Write(Blocks);
            e.Writer.Write(Scans);
            e.Writer.Write(MapPosition);
        }
    }
}