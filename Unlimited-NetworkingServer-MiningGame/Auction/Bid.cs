using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Auction
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

        public void AddBid(ushort userId, uint amount)
        {
            Id += 1;
            UserId = userId;
            Amount = Amount;
        }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            throw new System.NotImplementedException();
        }
    }
}