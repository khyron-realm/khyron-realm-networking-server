using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    public class Robot : IDarkRiftSerializable
    {
        public ushort Id { get; set; }
        public string Name { get; set; }
        public ushort Energy { get; set; }
        public ushort Propulsion { get; set; }
        public ushort Drill { get; set; }

        /// <summary>
        ///     Deserialization method for robot data
        /// </summary>
        /// <param name="e">Deserialize event</param>
        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt16();
            Name = e.Reader.ReadString();
            Energy = e.Reader.ReadUInt16();
            Propulsion = e.Reader.ReadUInt16();
            Drill = e.Reader.ReadUInt16();
        }

        /// <summary>
        ///     Serialization method for robot data
        /// </summary>
        /// <param name="e">Serialize event</param>
        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(Energy);
            e.Writer.Write(Propulsion);
            e.Writer.Write(Drill);
        }
    }
}