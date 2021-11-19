using System;
using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Mines
{
    public class ResourcesData : IDarkRiftSerializable
    {
        public int GenerationSeed { get; set; }

        public ResourcesData(ushort offset)
        {
            GenerationSeed = Environment.TickCount + offset;
        }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(GenerationSeed);
        }
    }
}