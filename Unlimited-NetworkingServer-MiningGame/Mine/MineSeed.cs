using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Mine
{
    /// <summary>
    ///     Mine seed values
    /// </summary>
    public class MineSeed : IDarkRiftSerializable
    {
        private short Global { get; }
        private short Silicon { get; }
        private short Lithium { get; }
        private short Titanium { get; }
        
        public MineSeed(short global, short silicon, short lithium, short titanium)
        {
            Global = global;
            Silicon = silicon;
            Lithium = lithium;
            Titanium = titanium;
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