using DarkRift;
using MongoDB.Bson.Serialization.Attributes;

namespace Unlimited_NetworkingServer_MiningGame.Mines
{
    /// <summary>
    ///     Mine block
    /// </summary>
    public class MineScan : IDarkRiftSerializable
    {
        [BsonId]
        public string Player { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        
        public MineScan()
        { }

        public MineScan(string player, ushort x, ushort y)
        {
            Player = player;
            X = x;
            Y = y;
        }
        
        public void Deserialize(DeserializeEvent e)
        {
            Player = e.Reader.ReadString();
            X = e.Reader.ReadUInt16();
            Y = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(X);
            e.Writer.Write(Y);
        }
    }
}