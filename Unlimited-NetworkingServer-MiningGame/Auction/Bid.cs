using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Auction
{
    /// <summary>
    ///     Auction bid data
    /// </summary>
    public class Bid : IDarkRiftSerializable
    { 
        public uint Id { get; set; }
        public uint UserId { get; set; }
        public string Username { get; set; }
        public uint Amount { get; set; }

        public Bid()
        { }
        
        public Bid(uint id, uint userId, string username, uint amount)
        {
            Id = id;
            UserId = userId;
            Username = username;
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
        /// <param name="username">The user name</param>
        /// <param name="amount">The amount</param>
        public void AddBid(uint userId, string username, uint amount)
        {
            Id += 1;
            UserId = userId;
            Username = username;
            Amount = amount;
        }
    }
}