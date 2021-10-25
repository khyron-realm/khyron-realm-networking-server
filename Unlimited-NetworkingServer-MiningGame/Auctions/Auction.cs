using System;
using DarkRift;
using Unlimited_NetworkingServer_MiningGame.Mines;

namespace Unlimited_NetworkingServer_MiningGame.Auctions
{
    /// <summary>
    /// 
    /// </summary>
    public class Auction : IDarkRiftSerializable
    {
        public Auction(uint id, Mine mine, Bid[] bids, long startTime, long endTime, uint startingPrice, uint increasePrice)
        {
            Id = id;
            Mine = mine;
            Bids = bids;
            StartTime = startTime;
            EndTime = endTime;
            StartingPrice = startingPrice;
            IncreasePrice = increasePrice;
        }

        public Auction()
        {
            throw new NotImplementedException();
        }

        public uint Id { get; set; }

        public Mine Mine { get; set; }
        public Bid[] Bids { get; set; }
        
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        
        public uint StartingPrice { get; set; }
        public uint IncreasePrice { get; set; }
        
        public void Deserialize(DeserializeEvent e)
        {
            throw new System.NotImplementedException();
        }

        public void Serialize(SerializeEvent e)
        {
            throw new System.NotImplementedException();
        }
    }
}