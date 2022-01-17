using DarkRift;
using MongoDB.Bson.Serialization.Attributes;

namespace Unlimited_NetworkingServer_MiningGame.Headquarters
{
    /// <summary>
    ///     Stores the player data
    /// </summary>
    public class PlayerData : IDarkRiftSerializable
    {
        [BsonId]
        public string Id { get; set; }
        public byte Level { get; set; }
        public uint Experience { get; set; }
        public uint Energy { get; set; }
        public Resource[] Resources { get; set; }
        public Robot[] Robots { get; set; }
        public BuildTask[] ConversionQueue { get; set; }
        public BuildTask[] UpgradeQueue { get; set; }
        public BuildTask[] BuildQueue { get; set; }
        public BackgroundTask[] BackgroundTasks { get; set; }

        public PlayerData() { }

        public PlayerData(string id, byte level, uint experience, uint energy, Resource[] resources, Robot[] robots,
            BuildTask[] conversionQueue, BuildTask[] upgradeQueue, BuildTask[] buildQueue, BackgroundTask[] backgroundTasks)
        {
            Id = id;
            Level = level;
            Experience = experience;
            Energy = energy;
            Resources = resources;
            Robots = robots;
            ConversionQueue = conversionQueue;
            UpgradeQueue = upgradeQueue;
            BuildQueue = buildQueue;
            BackgroundTasks = backgroundTasks;
        }
        
        /// <summary>
        ///     Deserialization method for player data
        /// </summary>
        /// <param name="e">Deserialize event</param>
        public void Deserialize(DeserializeEvent e)
        { }
        
        /// <summary>
        ///     Serialization method for player data
        /// </summary>
        /// <param name="e">Serialize event</param>
        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Level);
            e.Writer.Write(Experience);
            e.Writer.Write(Energy);
            e.Writer.Write(Resources);
            e.Writer.Write(Robots);
            e.Writer.Write(ConversionQueue);
            e.Writer.Write(UpgradeQueue);
            e.Writer.Write(BuildQueue);
            e.Writer.Write(BackgroundTasks);
        }
    }
}