using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    public class PlayerData: IDarkRiftSerializable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ushort Level { get; set; }
        public ushort Experience { get; set; }
        public ushort Energy { get; set; }
        
        public PlayerData(string id, string name, ushort level, ushort experience, ushort energy)
        {
            Id = id;
            Name = name;
            Level = level;
            Experience = experience;
            Energy = energy;
        }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadString();
            Name = e.Reader.ReadString();
            Level = e.Reader.ReadUInt16();
            Experience = e.Reader.ReadUInt16();
            Energy = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(Level);
            e.Writer.Write(Experience);
            e.Writer.Write(Energy);
        }
    }
}