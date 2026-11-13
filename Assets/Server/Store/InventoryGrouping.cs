using System;
using System.Collections.Generic;

namespace NsoServer.Store
{
    // Offline shared ordering policy: invoked only by the explicit bag/box Sort commands.
    // Client and server use the same keys; instance data and stack compatibility stay with callers.
    public static class InventoryGrouping
    {
        public const int MaxStackQuantity = short.MaxValue; // box quantities use a signed short
        // Existing stackable supplies plus food, star stones, crystals and insurance.
        // Equipment/mount instances remain separate even if an old template has a stack flag.
        public static bool CanStack(int type, bool originalStackable)
        {
            return type > 15 && (type < 29 || type > 38) && (originalStackable || type == 18 || type == 22 || type == 26 || type == 28);
        }

        public static void Group<T>(T[] slots, Func<T, int> type, Func<T, int> template, Func<T, bool> locked)
            where T : class
        {
            var entries = new List<KeyValuePair<int, T>>();
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] != null)
                    entries.Add(new KeyValuePair<int, T>(i, slots[i]));
            entries.Sort((a, b) =>
            {
                int order = type(a.Value).CompareTo(type(b.Value));
                if (order == 0)
                    order = template(a.Value).CompareTo(template(b.Value));
                if (order == 0)
                    order = locked(a.Value).CompareTo(locked(b.Value));
                // Keep distinct equipment instances in their original relative order.
                return order == 0 ? a.Key.CompareTo(b.Key) : order;
            });
            Array.Clear(slots, 0, slots.Length);
            for (int i = 0; i < entries.Count; i++)
                slots[i] = entries[i].Value;
        }
    }
}
