namespace Unlimited_NetworkingServer_MiningGame.Tags
{
    /// <summary>
    ///     
    /// </summary>
    public static class AuctionTags
    {
        private const ushort Shift = Tags.Auctions * Tags.TagsPerPlugin;

        public const ushort Create = 0 + Shift;
        public const ushort Join = 1 + Shift;
        public const ushort Leave = 2 + Shift;
        
        public const ushort GetOpenRooms = 3 + Shift;
        public const ushort GetOpenRoomsFailed = 4 + Shift;
        
        public const ushort CreateSuccess = 5 + Shift;
        public const ushort CreateFailed = 6 + Shift;
        
        public const ushort JoinSuccess = 7 + Shift;
        public const ushort JoinFailed = 8 + Shift;
        public const ushort LeaveSuccess = 9 + Shift;
        
        public const ushort PlayerJoined = 10 + Shift;
        public const ushort PlayerLeft = 11 + Shift;

        public const ushort StartAuction = 12 + Shift;
        public const ushort StartAuctionSuccess = 13 + Shift;
        public const ushort StartAuctionFailed = 14 + Shift;
    }
}