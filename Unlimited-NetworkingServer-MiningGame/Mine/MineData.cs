using System;
using System.Linq;
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
        public ushort Id { get; set; }
        public ushort Size { get; set; }
        public MineGenerationValues GenerationValues { get; set; }
        public bool[] BlocksValues { get; set; }
        public MineScan[] Scans { get; set; }
        public string Winner { get; set; }

        public MineData()
        { }

        public MineData(ushort id, ushort size)
        {
            Id = id;
            Size = size;
            GenerationValues = new MineGenerationValues();
            BlocksValues = new bool[] { };
            Scans = new MineScan[] { };
            Winner = "";
        }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt16();
            Size = e.Reader.ReadUInt16();
            GenerationValues = e.Reader.ReadSerializable<MineGenerationValues>();
            BlocksValues = e.Reader.ReadBooleans();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Size);
            e.Writer.Write(GenerationValues);
        }
    }
}