using DarkRift;
using MongoDB.Bson.Serialization.Attributes;

namespace Unlimited_NetworkingServer_MiningGame.GameElements
{
    public class GameParameters : IDarkRiftSerializable
    {
        public GameParameters()
        {
        }

        public GameParameters(ushort version, byte nrRobots, byte nrResources, byte maxQueueTasks, byte maxPlayerLevel,
            byte maxRobotsLevel, ushort maxExperience, uint maxEnergy, uint maxResources, Resource[] resources,
            Robot[] robots)
        {
            Version = version;
            NrRobots = nrRobots;
            NrResources = nrResources;
            MaxQueueTasks = maxQueueTasks;
            MaxPlayerLevel = maxPlayerLevel;
            MaxRobotsLevel = maxRobotsLevel;
            MaxExperience = maxExperience;
            MaxEnergy = maxEnergy;
            MaxResources = maxResources;
            Resources = resources;
            Robots = robots;
        }

        [BsonId]
        public ushort Version { get; set; }
        public byte NrRobots { get; set; }
        public byte NrResources { get; set; }

        public byte MaxQueueTasks { get; set; }

        public byte MaxPlayerLevel { get; set; }
        public byte MaxRobotsLevel { get; set; }
        public ushort MaxExperience { get; set; }
        public uint MaxEnergy { get; set; }
        public uint MaxResources { get; set; }
        public Resource[] Resources { get; set; }
        public Robot[] Robots { get; set; }

        #region Serialization

        public void Deserialize(DeserializeEvent e)
        {
            throw new System.NotImplementedException();
        }

        public void Serialize(SerializeEvent e)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}