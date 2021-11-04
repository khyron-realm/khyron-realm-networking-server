using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using DarkRift;
using DarkRift.Server;
using MongoDB.Bson.Serialization.Attributes;
using Unlimited_NetworkingServer_MiningGame.Game;
using Unlimited_NetworkingServer_MiningGame.Mines;
using Timer = System.Timers.Timer;

namespace Unlimited_NetworkingServer_MiningGame.Auction
{
    /// <summary>
    ///     Auction room details
    /// </summary>
    public class AuctionRoom : IDarkRiftSerializable
    {
        [BsonId]
        public uint Id { get; set; }
        public string Name { get; set; }
        public bool HasStarted { get; set; }
        public long EndTime { get; set; }
        public MineGenerator MineValues { get; set; }
        public List<MineScan> MineScans { get; set; }
        public Bid LastBid { get; set; }
        
        [BsonIgnore]
        public IClient LastBidderClient { get; set; }
        [BsonIgnore]
        public IClient OverbiddedClient { get; set; }
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
        
        public AuctionRoom()
        { }

        public AuctionRoom(uint id, string name)
        {
            Id = id;
            Name = name;
            OverbiddedClient = null;
            HasStarted = false;
            EndTime = 0;
            MineValues = new MineGenerator();
            MineScans = new List<MineScan>();
            LastBid = new Bid(0, "", Constants.InitialBid);
            LastBidderClient = null;
            OverbiddedClient = null;
        }

        public void Deserialize(DeserializeEvent e)
        { }

        public void Serialize(SerializeEvent e)
        {
            e.Writer.Write(Id);
            e.Writer.Write(Name);
            e.Writer.Write(HasStarted);
            e.Writer.Write(EndTime);
            byte playerCount;
            try
            {
                if (PlayerList != null) playerCount = (byte) PlayerList.Count;
                else playerCount = 0;
            }
            catch (Exception)
            {
                playerCount = 0;
            }
            e.Writer.Write(playerCount);
            e.Writer.Write(MineValues);
            e.Writer.Write(LastBid);
        }

        /// <summary>
        ///     Starts the auction and activates the end timer
        /// </summary>
        internal void StartAuction(int delay)
        {
            HasStarted = true;

            DateTime scheduledTime = DateTime.Now.AddMinutes(Constants.AuctionDuration).AddSeconds(delay);
            //DateTime scheduledTime = DateTime.Now.AddSeconds(10);
            EndTime = scheduledTime.ToBinary();
            double tickTime = (double)(scheduledTime - DateTime.Now).TotalMilliseconds;
            
            endTimer = new Timer(tickTime);
            endTimer.Elapsed += new ElapsedEventHandler(AuctionFinished);
            endTimer.AutoReset = false;
            endTimer.Start();
        }

        /// <summary>
        ///     Auction finished event
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="e">The event args</param>
        private void AuctionFinished(object sender, ElapsedEventArgs e)
        {
            endTimer.Stop();
            
            AuctionFinishedEventArgs args = new AuctionFinishedEventArgs
            {
                AuctionId = Id,
                Name = Name,
                Owner = LastBid.PlayerName
            };
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
            if (PlayerList.Count >= Constants.MaxAuctionPlayers)
                return false;
            
            PlayerList.Add(player);
            Clients.Add(client);
            return true;
        }

        /// <summary>
        ///     Removes a player from a room
        /// </summary>
        /// <param name="playerName">The username of the player</param>
        /// <param name="client">The client object</param>
        /// <returns>True if the player is removed and false otherwise</returns>
        internal bool RemovePlayer(IClient client, string playerName)
        {
            if (PlayerList.All(p => p.Name != playerName) && !Clients.Contains(client))
                return false;

            PlayerList.Remove(PlayerList.Find(p => p.Name == playerName));
            Clients.Remove(client);
            return true;
        }

        /// <summary>
        ///     Adds a bid to the auction
        /// </summary>
        /// <param name="username">The player username</param>
        /// <param name="amount">The amount of the new bid</param>
        /// <param name="client">The client object</param>
        /// <returns>True if the bid is added or false otherwise</returns>
        internal bool AddBid(string username, uint amount, IClient client)
        {
            if (PlayerList.All(p => p.Name != username) && !Clients.Contains(client))
                return false;

            lock (BidLock)
            {
                if (LastBid.Amount < amount)
                {
                    if (LastBidderClient != null)
                    {
                        OverbiddedClient = LastBidderClient;
                    }
                    LastBid.AddBid(username, amount);
                    LastBidderClient = client;
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        ///     Adds a scan to the auction mine
        /// </summary>
        /// <param name="scan">The scan made by the players</param>
        /// <param name="client">The client object</param>
        /// <param name="playerName">The username of the player</param>
        /// <returns>True if the bid is added or false otherwise</returns>
        internal bool AddScan(MineScan scan, IClient client, string playerName)
        {
            if (PlayerList.All(p => p.Name != playerName) && !Clients.Contains(client))
                return false;

            MineScans.Add(scan);
            return true;
        }
    }
    
    public class AuctionFinishedEventArgs : EventArgs
    {
        public uint AuctionId { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
    }
    
    public static class Extensions
    {
        public static T[] Append<T>(this T[] array, T item)
        {
            if (array == null || array.Length == 0) 
            {
                return new T[] { item };
            }
            Array.Resize(ref array, array.Length + 1);
            array[array.Length - 1] = item;
 
            return array;
        }
    }
}