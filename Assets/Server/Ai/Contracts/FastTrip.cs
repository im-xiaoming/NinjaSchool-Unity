using System.Collections.Generic;

namespace NsoServer.Ai
{
    /// <summary>A fast trip towards a travel target: by the carriage NPC (Umayaki) or a teleport scroll, to Dest.</summary>
    public sealed class FastTrip
    {
        public int Target, Dest, Npc = -1, BagIndex = -1, MenuIndex = -1, Saved;
        public string Label = "";
    }
}
