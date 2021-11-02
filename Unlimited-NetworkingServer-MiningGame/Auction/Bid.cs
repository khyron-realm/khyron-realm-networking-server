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

        public Bid()
        { }
        
        public Bid(ushort id, ushort userId, uint amount)
        {
            Id = id;
            UserId = userId;
            Amount = amount;
        }
        
        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(UserId);
            e.Writer.Write(Amount);
        }
        
        /// <summary>
        ///     Adds a bew bid
        /// </summary>
        /// <param name="userId">The user that made the bid</param>
        /// <param name="amount">The amount</param>
        public void AddBid(ushort userId, uint amount)
        {
            Id += 1;
            UserId = userId;
            Amount = Amount;
        }

    }
}