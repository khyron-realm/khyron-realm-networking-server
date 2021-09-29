namespace Unlimited_NetworkingServer_MiningGame.Tags
{
    public static class LoginTags
    {
        private const ushort Shift = Tags.Login * Tags.TagsPerPlugin;
        
        public const ushort LoginPlayer = 0 + Shift;
        public const ushort LogoutPlayer = 1 + Shift;
        public const ushort AddPlayer = 2 + Shift;
        public const ushort LoginSuccess = 3 + Shift;
        public const ushort LoginFailed = 4 + Shift;
        public const ushort LogoutSuccess = 5 + Shift;
        public const ushort AddPlayerSuccess = 6 + Shift;
        public const ushort AddPlayedFailed = 7 + Shift;
    }
}