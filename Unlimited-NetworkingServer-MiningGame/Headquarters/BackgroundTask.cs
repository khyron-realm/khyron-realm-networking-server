using System;
using DarkRift;
using MongoDB.Bson.Serialization.Attributes;

namespace Unlimited_NetworkingServer_MiningGame.Headquarters
{
    /// <summary>
    ///     Background task for player data
    /// </summary>
    public class BackgroundTask : IDarkRiftSerializable
    {
        public long Time { get; set; }
        public byte Type { get; set; }
        public uint ValueId { get; set; }
        public string ValueDescription { get; set; }

        public BackgroundTask()
        { }

        public BackgroundTask(byte type, uint valueId, string valueDescription)
        {
            Time = DateTime.UtcNow.ToBinary();
            Type = type;
            ValueId = valueId;
            ValueDescription = valueDescription;
        }

        /// <summary>
        ///     Deserialization method for build task
        /// </summary>
        /// <param name="e">Deserialize event</param>
        public void Deserialize(DeserializeEvent e)
        { }
        
        /// <summary>
        ///     Serialization method for build task
        /// </summary>
        /// <param name="e">Serialize event</param>
        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Time);
            e.Writer.Write(Type);
            e.Writer.Write(ValueId);
            e.Writer.Write(ValueDescription);
        }
    }
}