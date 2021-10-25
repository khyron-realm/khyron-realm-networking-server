using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Mines
{
    /// <summary>
    /// 
    /// </summary>
    public class Mine : IDarkRiftSerializable
    {
        public uint Id { get; set; }
        
        public uint Size { get; set; }
        
        public byte[][] Blocks { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            throw new System.NotImplementedException();
        }

        public void Serialize(SerializeEvent e)
        {
            throw new System.NotImplementedException();
        }
    }
}