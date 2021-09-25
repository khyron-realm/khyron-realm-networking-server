namespace Unlimited_NetworkingServer_MiningGame
{
    public class Player
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
    }
}