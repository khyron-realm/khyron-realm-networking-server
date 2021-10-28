using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Mine
{
    /// <summary>
    ///     Mine block
    /// </summary>
    public class Block : IDarkRiftSerializable
    {
        private ushort X { get; }
        private ushort Y { get; }
        
        public Block(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }
        
        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(X);
            e.Writer.Write(Y);
        }
    }
}