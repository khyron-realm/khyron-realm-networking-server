using DarkRift;
using MongoDB.Bson.Serialization.Attributes;

namespace Unlimited_NetworkingServer_MiningGame.Game
{
    public class GameData : IDarkRiftSerializable
    {
        public GameData(ushort version)
        {
            Version = version;
        }

        public GameData() { }

        [BsonId]
        public ushort Version { get; set; }

        #region Serialization

        public void Deserialize(DeserializeEvent e)
        {
            Version = e.Reader.ReadUInt16();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Version);
        }

        #endregion
    }
}