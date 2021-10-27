using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame.Auctions
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
        public bool IsVisible { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }

        public List<IClient> Clients = new List<IClient>();
        public List<Player> PlayerList = new List<Player>();
        
        public AuctionRoom(ushort id, string name, bool isVisible, long startTime, long endTime)
        {
            Id = id;
            Name = name;
            HasStarted = false;
            IsVisible = isVisible;
            StartTime = startTime;
            EndTime = endTime;
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
        /// <param name="player">The player which makes the bid</param>
        /// <param name="client">The client object</param>
        /// <returns></returns>
        public bool AddBid(Player player, IClient client)
        {
            if (PlayerList.All(p => p.Id != client.ID) && !Clients.Contains(client))
                return false;
            /*
            Bid newBid = new Bid(0, player.Id, );
            int bidListSize = Auction.Bids.Length;
            Auction.Bids[bidListSize] = newBid;
            */
            return true;
        }
    }
}