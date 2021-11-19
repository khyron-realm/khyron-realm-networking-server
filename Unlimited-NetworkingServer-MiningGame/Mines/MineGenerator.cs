using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Mines
{
    /// <summary>
    ///     Mine seed values
    /// </summary>
    public class MineGenerator : IDarkRiftSerializable
    {
        public ResourcesData Global { get; set; }
        public ResourcesData Silicon { get; set; }
        public ResourcesData Lithium { get; set; }
        public ResourcesData Titanium { get; set; }
        
        public MineGenerator()
        {
            Global = new ResourcesData(16020);
            Silicon = new ResourcesData(30);
            Lithium = new ResourcesData(8234);
            Titanium = new ResourcesData(65007);
        }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Global);
            e.Writer.Write(Silicon);
            e.Writer.Write(Lithium);
            e.Writer.Write(Titanium);
        }
    }
}