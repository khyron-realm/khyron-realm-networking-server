using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;
using Unlimited_NetworkingServer_MiningGame.Mine;

namespace Unlimited_NetworkingServer_MiningGame.Auction
{
    /// <summary>
    ///     Auction room details
    /// </summary>
    public class AuctionRoom : IDarkRiftSerializable
    {
        public ushort Id { get; }
        public string Name { get; }
        public byte MaxPlayers { get; } = 50;
        public bool HasStarted { get; set; }
        public bool IsVisible { get; }
        public MineData Mine { get; set; }
        public Bid LastBid { get; set; }
        public long EndTime { get; set; }

        public List<IClient> Clients = new List<IClient>();
        public List<Player> PlayerList = new List<Player>();
        
        private static readonly object BidLock = new object();
        
        public AuctionRoom(ushort id, string name, bool isVisible, MineData mine)
        {
            Id = id;
            Name = name;
            HasStarted = false;
            IsVisible = isVisible;
            Mine = mine;
            LastBid = new Bid(0, 0, AuctionConstants.InitialBid);
            EndTime = 0;
        }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(MaxPlayers);
            e.Writer.Write((byte) PlayerList.Count);
        }

        /// <summary>
        ///     Adds a player to a room
        /// </summary>
        /// <param name="player">The player to be added</param>
        /// <param name="client">The client object</param>
        /// <returns>True if the player is added or false otherwise</returns>
        internal bool AddPlayer(Player player, IClient client)
        {
            if (PlayerList.Count >= MaxPlayers || HasStarted)
                return false;
            
            PlayerList.Add(player);
            Clients.Add(client);
            return true;
        }

        /// <summary>
        ///     Removes a player from a room
        /// </summary>
        /// <param name="client">The client object</param>
        /// <returns></returns>
        internal bool RemovePlayer(IClient client)
        {
            if (PlayerList.All(p => p.Id != client.ID) && !Clients.Contains(client))
                return false;

            PlayerList.Remove(PlayerList.Find(p => p.Id == client.ID));
            Clients.Remove(client);
            return true;
        }

        /// <summary>
        ///     Adds a bid to the auction
        /// </summary>
        /// <param name="playerId">The player id of the bidder</param>
        /// <param name="amount">The amount of the new bid</param>
        /// <param name="client">The client object</param>
        /// <returns></returns>
        public bool AddBid(ushort playerId, uint amount, IClient client)
        {
            if (PlayerList.All(p => p.Id != client.ID) && !Clients.Contains(client))
                return false;

            lock (BidLock)
            {
                if (LastBid.Amount < amount)
                {
                    LastBid.AddBid(playerId, amount);
                    return true;
                }
            }
            
            return false;
        }
    }
}