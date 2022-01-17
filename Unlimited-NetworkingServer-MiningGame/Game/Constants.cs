namespace Unlimited_NetworkingServer_MiningGame.Game
{
    public static class Constants
    {
        #region PlayerData

        public const ushort InitialEnergy = 10000;
        public const ushort InitialSilicon = 1000;
        public const ushort InitialLithium = 450;
        public const ushort InitialTitanium = 250;

        #endregion
        
        #region Auction

        public const uint InitialBid = 500;
        public const uint IncrementBid = 100;
        
        public const byte MaxAuctionPlayers = 20;
        public const uint AuctionDurationMinutes = 5;
        public const ushort InitialNrAuctions = 6;

        public const ushort TopUpEnergyValue = 5000;
        public const ushort TopUpHour = 12;
        public const ushort TopUpMinute = 0;
        public const ushort TopUpIntervalInHours = 6;

        #endregion

        #region Mine

        public const byte NrMineScans = 3;
        public const ushort MineSize = 900;

        #endregion
    }
}