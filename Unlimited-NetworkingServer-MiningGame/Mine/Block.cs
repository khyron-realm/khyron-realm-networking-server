using DarkRift;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Unlimited_NetworkingServer_MiningGame.Mine
{
    /// <summary>
    ///     Mine block
    /// </summary>
    public class Block : IDarkRiftSerializable
    {
        public ushort X { get; set; }
        public ushort Y { get; set; }
        
        public Block(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

        public Block()
        { }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(X);
            e.Writer.Write(Y);
        }
    }
}