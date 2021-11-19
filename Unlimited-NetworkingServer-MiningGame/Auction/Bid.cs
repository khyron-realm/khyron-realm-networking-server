using DarkRift;

namespace Unlimited_NetworkingServer_MiningGame.Auction
{
    /// <summary>
    ///     Auction bid data
    /// </summary>
    public class Bid : IDarkRiftSerializable
    { 
        public ushort Id { get; set; }
        public string PlayerName { get; set; }
        public uint Amount { get; set; }

        public Bid()
        { }
        
        public Bid(ushort id, string playerName, uint amount)
        {
            Id = id;
            PlayerName = playerName;
            Amount = amount;
        }
        
        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(PlayerName);
            e.Writer.Write(Amount);
        }

        /// <summary>
        ///     Adds a bew bid
        /// </summary>
        /// <param name="playerName">The user name</param>
        /// <param name="amount">The amount</param>
        public void AddBid(string playerName, uint amount)
        {
            Id += 1;
            PlayerName = playerName;
            Amount = amount;
        }
    }
}