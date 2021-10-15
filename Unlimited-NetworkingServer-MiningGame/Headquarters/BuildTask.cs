using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Headquarters
{
    /// <summary>
    ///     Build task for player data
    /// </summary>
    public class BuildTask : IDarkRiftSerializable
    {
        public byte Id { get; set; }
        public byte Type { get; set; }
        public byte Element { get; set; }
        public long StartTime { get; set; }

        public BuildTask()
        {
            Id = 0;
            Type = 0;
            Element = 0;
            StartTime = 0;
        }
  
        public BuildTask(byte id, byte type, byte element, long startTime)
        {
            Id = id;
            Type = type;
            Element = element;
            StartTime = startTime;
        }

        /// <summary>
        ///     Deserialization method for build task
        /// </summary>
        /// <param name="e">Deserialize event</param>
        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadByte();
            Type = e.Reader.ReadByte();
            Element = e.Reader.ReadByte();
            StartTime = e.Reader.ReadInt64();
        }
        
        /// <summary>
        ///     Serialization method for build task
        /// </summary>
        /// <param name="e">Serialize event</param>
        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Type);
            e.Writer.Write(Element);
            e.Writer.Write(StartTime);
        }
    }
}