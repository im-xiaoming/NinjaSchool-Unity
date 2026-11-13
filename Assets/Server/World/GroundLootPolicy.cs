namespace NsoServer.World
{
    using Math = System.Math;

    /// <summary>Shared ground-item timing for every character. Java ItemMap.isCanPickup/isExpired;
    /// offline deviation requested by the user: 30 s exclusive ownership and 60 s ground lifetime.</summary>
    public static class GroundLootPolicy
    {
        public static int OwnerProtectionMs { get; private set; } = 30000;
        public static int LifetimeMs { get; private set; } = 60000;

        // Applied before the server starts, not per session: bots and humans have exactly the same rules.
        public static void Configure(int ownerSeconds, int lifetimeSeconds)
        {
            ownerSeconds = Math.Max(0, Math.Min(600, ownerSeconds));
            OwnerProtectionMs = ownerSeconds * 1000;
            LifetimeMs = Math.Max(ownerSeconds + 1, Math.Min(3600, lifetimeSeconds)) * 1000;
        }

        public static bool HasOwnership(int characterId, ItemMapEntry item, long now)
        {
            return item.OwnerId == -1 || item.OwnerId == characterId || now - item.CreatedAt >= OwnerProtectionMs;
        }
    }
}
