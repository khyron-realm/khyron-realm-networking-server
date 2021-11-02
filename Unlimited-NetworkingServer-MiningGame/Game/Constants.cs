namespace Unlimited_NetworkingServer_MiningGame.Game
{
    public static class Constants
    {
        #region PlayerData

        public const ushort InitialEnergy = 10000;

        #endregion
        
        #region Auction

        public const uint InitialBid = 500;
        public const uint IncrementBid = 100;
        
        public const byte MaxAuctionPlayers = 20;
        public const uint AuctionDuration = 10;         // minutes
        public const ushort InitialNrAuctions = 6;
        public const ushort MineSize = 900;

        #endregion

        #region Mine

        public const byte NrMineScans = 3;

        #endregion
    }
}