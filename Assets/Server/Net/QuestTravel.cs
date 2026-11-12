using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math;
    using TileMap = NsoServer.World.TileMap;

    // Offline dev-terminal convenience requested by the user. Resolve the current objective from
    // GameScr.getTaskMapId / Char.getTaskNpcId and existing quest data; never call TaskNext/FinishTask.
    public static class QuestTravel
    {
        public static string GoToObjective(PlayerState me, GameWorld world)
        {
            if (me.IsDead) return "Không thể dùng /goto khi đã chết.";
            if (me.CatchUntil > 0 || me.FishUntil > 0) return "Hãy đợi thao tác hiện tại hoàn tất trước khi dùng /goto.";
            var task = world.FindTask(me.Record.TaskId);
            var maps = world.TaskMaps(me.Record.TaskId);
            int step = me.Progress.TaskIndex;
            int entry = me.Progress.TaskActive ? step + 1 : 0;
            if (task == null) return "Không còn nhiệm vụ chính nào.";
            if (maps == null || entry < 0 || entry >= maps.Length) return "Không tìm thấy địa điểm cho bước nhiệm vụ này.";
            int mapId = maps[entry];
            // Same class-specific school resolution as GameScr.getTaskMapId.
            if (mapId == -1)
                mapId = me.Record.ClassId == 0 && task.Id == PlayerTasks.TaskGiaTangSucManh ? -2
                    : me.Record.ClassId <= 2 ? 1 : me.Record.ClassId <= 4 ? 72 : 27;

            int mobId = -1;
            if (me.Progress.TaskActive && step >= 0)
            {
                if (step < task.KillMobs.Length && task.KillMobs[step].Length > 0)
                    mobId = task.KillMobs[step][0];
                if (mobId < 0)
                    foreach (var drop in PlayerTasks.TaskDropList())
                        if (drop[1] == task.Id && drop[2] == step) { mobId = drop[0]; break; }
            }
            // Some kill steps have map=-2 (any suitable map). Prefer the current map, then a
            // deterministic normal map containing the required spawn; do not pick an arbitrary mob.
            if (mapId < 0 && mobId >= 0)
            {
                var current = world.FindMap(me.MapId);
                if (HasMob(current, mobId)) mapId = current.Id;
                else
                    foreach (var map in world.MapTemplates())
                        if (map.IsNormal && HasMob(map, mobId) && (mapId < 0 || map.Id < mapId)) mapId = map.Id;
            }
            if (mapId < 0) return "Bước này không có địa điểm cố định. Hãy kiểm tra yêu cầu nhiệm vụ (cấp độ, trang bị hoặc vật phẩm).";
            var tile = world.FindMap(mapId);
            if (tile == null || !tile.IsNormal || world.RandomZoneId(mapId) < 0)
                return "Địa điểm này là khu vực đặc biệt. Hãy vào qua NPC hoặc vật phẩm nhiệm vụ.";

            // Offline (user request): arrive at the map's entrance like walking in through a gate, not on the quest
            // spot itself. The entrance is the arrival point of a waypoint leading here (Zone.requestChangeMap sets
            // x/y from the source waypoint); the quest spot below is only the fallback for maps without one.
            int x = -1, y = -1;
            EntryPoint(world, mapId, me.MapId, out x, out y);
            if (x < 0 && tile.LocationStand.Count > 0 && tile.LocationStand[0].Length >= 2)
            { x = tile.LocationStand[0][0]; y = tile.LocationStand[0][1]; }
            int npcId = PlayerTasks.TaskNpcId(me, world);
            if (x < 0)
                foreach (var npc in tile.Npcs)
                    if (npc.TemplateId == npcId) { x = npc.X + 24; y = npc.Y; break; }
            if (x < 0 && me.Progress.TaskActive)
                TaskMechanics.TryTravelPosition(me, world, tile, out x, out y);
            if (x < 0 && mobId >= 0)
                foreach (var mob in tile.Monsters)
                    if (mob.TemplateId == mobId) { x = mob.X - 48; y = mob.Y; break; }
            if (x < 0 && tile.LocationStand.Count > 0 && tile.LocationStand[0].Length >= 2)
            { x = tile.LocationStand[0][0]; y = tile.LocationStand[0][1]; }
            if (x < 0) return "Chưa xác định được vị trí đứng an toàn tại nơi làm nhiệm vụ.";

            var grid = world.GetGrid(tile);
            x = Math.Max(24, Math.Min(grid.PxW - 25, x));
            y = grid.CollisionY((short)x, (short)Math.Max(0, Math.Min(grid.PxH - 1, y)));
            // Reuse the existing map transition and hooks. Exploration steps can naturally progress
            // on entering their map; /goto itself does not accept, complete or skip any quest.
            me.SetXY((short)x, (short)y);
            me.PreX = (short)x; me.PreY = (short)y;
            if (me.MapId == mapId) me.Actions.ResetPoint();
            else me.Actions.ChangeMap(mapId);
            return "Đã tới " + tile.Name + " cho nhiệm vụ '" + task.Name + "'.";
        }

        /// <summary>The arrival point in <paramref name="mapId"/> of a waypoint leading to it: from the current map when
        /// there is one, otherwise from the lowest-numbered neighbour (deterministic). x = -1 when no map leads here.</summary>
        private static void EntryPoint(GameWorld world, int mapId, int fromMapId, out int x, out int y)
        {
            x = -1; y = -1;
            int bestFrom = int.MaxValue;
            foreach (var map in world.MapTemplates())
            {
                if (map.Id == mapId) continue;
                foreach (var wp in map.Waypoints)
                {
                    if (wp.Next != mapId) continue;
                    int rank = map.Id == fromMapId ? -1 : map.Id;
                    if (rank < bestFrom) { bestFrom = rank; x = wp.X; y = wp.Y; }
                }
            }
        }

        private static bool HasMob(TileMap tile, int id)
        {
            if (tile == null) return false;
            foreach (var mob in tile.Monsters) if (mob.TemplateId == id) return true;
            return false;
        }
    }
}
