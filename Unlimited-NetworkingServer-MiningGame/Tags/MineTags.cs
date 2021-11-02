namespace Unlimited_NetworkingServer_MiningGame.Tags
{
    /// <summary>
    ///     Tags for mine plugin
    /// </summary>
    public static class MineTags
    {
        private const ushort Shift = Tags.Mine * Tags.TagsPerPlugin;

        public const ushort RequestFailed = 0 + Shift;
    }
}