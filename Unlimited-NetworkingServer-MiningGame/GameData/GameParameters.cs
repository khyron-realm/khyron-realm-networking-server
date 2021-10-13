using DarkRift;
using MongoDB.Bson.Serialization.Attributes;

namespace Unlimited_NetworkingServer_MiningGame.GameData
{
    public class GameParameters : IDarkRiftSerializable
    {
        public GameParameters(ushort version, byte maxPlayerLevel, byte maxRobotsLevel, uint maxEnergy,
            ushort maxExperience, uint maxBuildRobotsQueue, ResourceDetails[] resources, RobotDetails[] robots)
        {
            Version = version;
            MaxPlayerLevel = maxPlayerLevel;
            MaxRobotsLevel = maxRobotsLevel;
            MaxEnergy = maxEnergy;
            MaxExperience = maxExperience;
            MaxBuildRobotsQueue = maxBuildRobotsQueue;
            Resources = resources;
            Robots = robots;
        }

        [BsonId]
        public ushort Version { get; set; }
        public byte MaxPlayerLevel { get; set; }
        public byte MaxRobotsLevel { get; set; }
        public uint MaxEnergy { get; set; }
        public ushort MaxExperience { get; set; }
        public uint MaxBuildRobotsQueue { get; set; }
        public ResourceDetails[] Resources { get; set; }
        public RobotDetails[] Robots { get; set; }
        
        // public LevelFormulas Levels { get; set; }

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