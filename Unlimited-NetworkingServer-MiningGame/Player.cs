using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame
{
    public class Player: IDarkRiftSerializable
    {
        public string id { get; set; }
        public string name { get; set; }
        
        public ushort level { get; set; }
        public ushort experience { get; set; }
        public ushort energy { get; set; }
        
        public Player(string id, string name, ushort level, ushort experience, ushort energy)
        {
            this.id = id;
            this.name = name;
            
            this.level = level;
            this.experience = experience;
            this.energy = energy;
        }

        public void Deserialize(DeserializeEvent e)
        {
            id = e.Reader.ReadString();
            name = e.Reader.ReadString();
            level = e.Reader.ReadUInt16();
            experience = e.Reader.ReadUInt16();
            energy = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(id);
            e.Writer.Write(name);
            e.Writer.Write(level);
            e.Writer.Write(experience);
            e.Writer.Write(energy);
        }
    }
}