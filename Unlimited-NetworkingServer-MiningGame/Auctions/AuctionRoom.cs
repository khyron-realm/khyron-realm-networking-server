using System.Collections.Generic;
using System.Linq;
using DarkRift;
using DarkRift.Server;

namespace Unlimited_NetworkingServer_MiningGame.Auctions
{
    /// <summary>
    ///     
    /// </summary>
    public class AuctionRoom : IDarkRiftSerializable
    {
        public ushort Id { get; set; }
        public string Name { get; set; }
        public byte MinPlayers { get; set; }
        public byte MaxPlayers { get; set; }
        public bool HasStarted { get; set; }
        public bool IsVisible { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public Auction Auction { get; set; }
        
        public List<IClient> Clients = new List<IClient>();
        public List<Player> PlayerList = new List<Player>();
        
        public AuctionRoom(ushort id, string name, bool hasStarted, bool isVisible, long startTime, long endTime, Auction auction)
        {
            Id = id;
            Name = name;
            HasStarted = hasStarted;
            IsVisible = isVisible;
            StartTime = startTime;
            EndTime = endTime;
            Auction = auction;
        }

        public void Deserialize(DeserializeEvent e)
        {
            Id = e.Reader.ReadUInt16();
            Name = e.Reader.ReadString();
            HasStarted = e.Reader.ReadBoolean();
            IsVisible = e.Reader.ReadBoolean();
            StartTime = e.Reader.ReadInt64();
            EndTime = e.Reader.ReadInt64();
            Auction = e.Reader.ReadSerializable<Auction>();
        }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(HasStarted);
            e.Writer.Write(IsVisible);
            e.Writer.Write(StartTime);
            e.Writer.Write(EndTime);
            e.Writer.Write(Auction);
        }

        internal bool AddPlayer(Player player, IClient client)
        {
            if (PlayerList.Count >= MaxPlayers || HasStarted)
                return false;
            
            PlayerList.Add(player);
            Clients.Add(client);
            return true;
        }

        internal bool RemovePlayer(IClient client)
        {
            if (PlayerList.All(p => p.Id != client.ID) && !Clients.Contains(client))
                return false;

            PlayerList.Remove(PlayerList.Find(p => p.Id == client.ID));
            Clients.Remove(client);
            return true;
        }

        public bool AddBid(Player player, IClient client, ushort roomId)
        {
            if (PlayerList.All(p => p.Id != client.ID) && !Clients.Contains(client))
                return false;
            
            Bid newBid = new Bid(player.Id, roomId, Auction.IncreasePrice);
            int bidListSize = Auction.Bids.Length;
            Auction.Bids[bidListSize] = newBid;
            return true;
        }
    }
}