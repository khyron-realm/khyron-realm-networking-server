using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Auction
{
    /// <summary>
    ///     Auction bid data
    /// </summary>
    public class Bid : IDarkRiftSerializable
    { 
        public ushort Id { get; set; }
        public ushort UserId { get; set; }
        public uint Amount { get; set; }
        
        public Bid(ushort id, ushort userId, uint amount)
        {
            Id = id;
            UserId = userId;
            Amount = amount;
        }

        public void AddBid(ushort userId, uint amount)
        {
            Id += 1;
            UserId = userId;
            Amount = Amount;
        }

        public void Deserialize(DeserializeEvent e)
        {
            
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(UserId);
            e.Writer.Write(Amount);
        }
    }
}