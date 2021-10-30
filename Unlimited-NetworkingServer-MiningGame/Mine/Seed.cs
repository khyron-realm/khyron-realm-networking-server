using DarkRift;
using MongoDB.Bson.Serialization.Attributes;

namespace Unlimited_NetworkingServer_MiningGame.Mine
{
    /// <summary>
    ///     Mine seed values
    /// </summary>
    public class MineSeed : IDarkRiftSerializable
    {
        public short Global { get; set; }
        public short Silicon { get; set; }
        public short Lithium { get; set; }
        public short Titanium { get; set; }
        
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