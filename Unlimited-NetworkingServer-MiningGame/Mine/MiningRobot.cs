using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Mine
{
    /// <summary>
    ///     Robot data when placed in the mine
    /// </summary>
    public class MiningRobot : IDarkRiftSerializable
    {
        public byte Id { get; set; }
        public long StartTime { get; set; }
        public Block StartingPosition { get; set; }

        public MiningRobot()
        { }
        
        public MiningRobot(byte id, long startTime, Block startingPosition)
        {
            Id = id;
            StartTime = startTime;
            StartingPosition = startingPosition;
        }

        /// <summary>
        ///     Deserialization method for robot data
        /// </summary>
        /// <param name="e">Deserialize event</param>
        public void Deserialize(DeserializeEvent e)
        { }

        /// <summary>
        ///     Serialization method for robot data
        /// </summary>
        /// <param name="e">Serialize event</param>
        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(StartTime);
            e.Writer.Write(StartingPosition);
        }
    }
}