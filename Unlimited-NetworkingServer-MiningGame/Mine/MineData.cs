using System.Xml.Schema;
using DarkRift;
using Unlimited_NetworkingServer_MiningGame.Game;

namespace Unlimited_NetworkingServer_MiningGame.Mine
{
    /// <summary>
    ///     Mine details
    /// </summary>
    public class MineData : IDarkRiftSerializable
    {
        public uint Id { get; }
        public uint Size { get; }
        public MineSeed Seed { get; }
        public Block[] Scans { get; set; }

        public MineData(uint id, uint size, MineSeed seed)
        {
            Id = id;
            Size = size;
            Seed = seed;
            Scans = new Block[Constants.NrMineScans];
        }
        
        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Size);
            e.Writer.Write(Seed);
        }
    }
}