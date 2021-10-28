using System.Xml.Schema;
using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Mine
{
    /// <summary>
    ///     Mine details
    /// </summary>
    public class MineData : IDarkRiftSerializable
    {
        public uint Id { get; set; }
        public uint Size { get; set; }
        public MineSeed Seed { get; set; }

        public MineData(uint id, uint size, MineSeed seed)
        {
            Id = id;
            Size = size;
            Seed = seed;
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