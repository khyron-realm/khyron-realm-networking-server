using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using DarkRift;
using DarkRift.Server;
using MongoDB.Bson.Serialization.Attributes;
using Unlimited_NetworkingServer_MiningGame.Game;
using Unlimited_NetworkingServer_MiningGame.Mine;
using Timer = System.Timers.Timer;

namespace Unlimited_NetworkingServer_MiningGame.Auction
{
    /// <summary>
    ///     Auction room details
    /// </summary>
    public class AuctionRoom : IDarkRiftSerializable
    {
        [BsonId]
        public ushort Id { get; set; }
        public string Name { get; set; }
        public byte MaxPlayers { get; }
        public bool HasStarted { get; set; }
        public bool IsVisible { get; set;  }
        public MineData Mine { get; set; }
        public Bid LastBid { get; set; }
        public long EndTime { get; set; }
        
        [BsonIgnore]
        public List<IClient> Clients = new List<IClient>();
        [BsonIgnore]
        public List<Player> PlayerList = new List<Player>();
        [BsonIgnore] private Timer endTimer;
        [BsonIgnore]
        private static readonly object BidLock = new object();
        
        public event EventHandler<AuctionFinishedEventArgs> OnAuctionFinished;
        
        protected virtual void OnThresholdReached(AuctionFinishedEventArgs e)
        {
            EventHandler<AuctionFinishedEventArgs> handler = OnAuctionFinished;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        
        public AuctionRoom(ushort id, string name, bool isVisible, MineData mine)
        {
            Id = id;
            Name = name;
            MaxPlayers = Constants.MaxAuctionPlayers;
            HasStarted = false;
            IsVisible = isVisible;
            Mine = mine;
            LastBid = new Bid(0, 0, Constants.InitialBid);
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
            e.Writer.Write(Mine);
            e.Writer.Write(LastBid);
            e.Writer.Write(EndTime);
        }

        /// <summary>
        ///     Starts the auction and activates the end timer
        /// </summary>
        internal void StartAuction()
        {
            HasStarted = true;

            DateTime scheduledTime = DateTime.Now.AddMinutes(Constants.AuctionDuration);
            EndTime = scheduledTime.ToBinary();
            double tickTime = (double)(scheduledTime - DateTime.Now).TotalMilliseconds;
            
            endTimer = new Timer(tickTime);
            endTimer.Elapsed += new ElapsedEventHandler(AuctionFinished);
            endTimer.AutoReset = false;
            endTimer.Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AuctionFinished(object sender, ElapsedEventArgs e)
        {
            endTimer.Stop();
            
            AuctionFinishedEventArgs args = new AuctionFinishedEventArgs();
            args.AuctionId = Id;
            args.endTime = EndTime;
            OnThresholdReached(args);
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
        /// <returns>True if the player is removed and false otherwise</returns>
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
        /// <returns>True if the bid is added or false otherwise</returns>
        internal bool AddBid(ushort playerId, uint amount, IClient client)
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

        /// <summary>
        ///     Adds a scan to the auction mine
        /// </summary>
        /// <param name="scanId">The scan number</param>
        /// <param name="block">The location of the scan</param>
        /// <param name="client">The client object</param>
        /// <returns>True if the scan is added or false otherwise</returns>
        internal bool AddScan(ushort scanId, Block block, IClient client)
        {
            if (PlayerList.All(p => p.Id != client.ID) && !Clients.Contains(client))
                return false;

            Mine.Scans = Mine.Scans.Append(block);
            return true;
        }
    }
    
    public class AuctionFinishedEventArgs : EventArgs
    {
        public ushort AuctionId { get; set; }
        public long endTime { get; set; }
    }
    
    public static class Extensions
    {
        public static T[] Append<T>(this T[] array, T item)
        {
            if (array == null || array.Length == 0) {
                return new T[] { item };
            }
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = item;
 
            return array;
        }
    }
}