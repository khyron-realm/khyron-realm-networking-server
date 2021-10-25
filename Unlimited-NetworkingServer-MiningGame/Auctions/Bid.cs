using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Auctions
{
    /// <summary>
    /// 
    /// </summary>
    public class Bid : IDarkRiftSerializable
    {
        public Bid(ushort id, ushort userId, uint amount)
        {
            Id = id;
            UserId = userId;
            Amount = amount;
        }

        public ushort Id { get; set; }
        public ushort UserId { get; set; }
        public uint Amount { get; set; }

        public void Deserialize(DeserializeEvent e)
        {
            throw new System.NotImplementedException();
        }

        public void Serialize(SerializeEvent e)
        {
            throw new System.NotImplementedException();
        }
    }
}