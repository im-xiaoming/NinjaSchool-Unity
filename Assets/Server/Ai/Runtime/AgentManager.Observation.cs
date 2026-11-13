using System;
using System.Collections.Generic;
using System.Text;
using NsoServer.Data;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;
    using Mob = NsoServer.World.Mob;
    using Zone = NsoServer.World.Zone;

    /// <summary>
    /// Offline AI players (offline addition; the Java server only had scripted Bot instances).
    /// Each AI player is a real character: its own account (AgentConfig.AccountPrefix + name) and character in the save
    /// database, logged in on a bot session (GameServer.ConnectBot) and driven with the messages a game client sends
    /// (BotClient), so every game rule applies to it exactly as to a human player and other players see it through the
    /// normal zone messages (Presence). It starts as a new character (level 1, quest 0).
    /// - Strategic AI decides WHAT and WHY: evaluated RL, with the utility planner as teacher/fallback. The shared
    ///   LLM service provides asynchronous advice and speech. Requests are event-driven and throttled, never per frame.
    /// - Tactical AI decides HOW: AgentTactics shares its skills with RL primitive selection every 500 ms.
    /// - Memory: recent events (Memory) and long-term gameplay knowledge (AgentKnowledge), saved in the save database
    ///   (document "ai_players") and given to the LLM with the character's bag, equipment, experience and quest.
    /// - Their actions, screen messages, decisions and states go to the bot log (PlayerLog.Bot, kept 20 days).
    /// They cannot use the dev terminal (Controller ignores CHAT_MAP commands from bot sessions).
    /// Everything except the LLM worker runs on the server tick thread under GameWorld.SyncRoot.
    /// </summary>
    public static partial class AgentManager
    {
        /// <summary>The planner's view of the AI player (copied, safe to use on the worker thread).</summary>
        internal static AgentSnapshot Snapshot(NinjaAgent a)
        {
            var p = a.P;
            var me = a.Me;
            int map = me.MapId;
            int task = me.Record.TaskId;
            long expLevel = world.ExpForLevel(me.Level), expNext = world.ExpForLevel(me.Level + 1);
            var s = new AgentSnapshot
            {
                AgentId = a.Index,
                Name = p.Name,
                Bio = p.Bio,
                Traits = p.Traits(),
                ClassName = me.Record.ClassId >= 0 && me.Record.ClassId < ClassNames.Length ? ClassNames[me.Record.ClassId] : "class " + me.Record.ClassId,
                Aggression = p.Aggression,
                Caution = p.Caution,
                Greed = p.Greed,
                Curiosity = p.Curiosity,
                Sociability = p.Sociability,
                Level = me.Level,
                HpPercent = AgentTactics.HpPercent(me),
                MpPercent = AgentTactics.MpPercent(me),
                Exp = me.Record.Exp,
                ExpNext = expNext,
                ExpPercent = expNext > expLevel ? Math.Max(0, Math.Min(100, (me.Record.Exp - expLevel) * 100.0 / (expNext - expLevel))) : 100,
                Kills = a.Kills,
                Deaths = a.Deaths,
                QuestsDone = a.QuestsDone,
                Yen = me.Progress.Yen,
                Xu = me.Progress.Xu,
                MapId = map,
                MapName = me.Zone.Tile.Name,
                Trigger = a.Trigger,
                PlayersHere = me.Zone.Players.Count - 1,
                InVillage = IsVillage(map),
                HpPotions = AgentTactics.HpPotions(me, world),
                MpPotions = AgentTactics.MpPotions(me, world),
                FreeSlots = PlayerItems.SlotNull(me),
                HasWeapon = me.Inventory.Equipment[1] != null,
                CanShop = task >= 3,
                CurrentPlan = a.Plan != null && a.Step != null ? a.Plan.Reason + " / step " + (a.StepIndex + 1) + " of " + a.Plan.Steps.Count + ": " + a.Step : "",
                UnlockedMaps = MapUnlock.Allowed(task).Count,
                Seed = Rng.Next()
            };
            s.CanAffordPotions = s.CanShop && (me.Progress.Yen >= 100 || me.Progress.Xu >= 100);
            long nowMs = Now();
            s.Foods = AgentTactics.Foods(me, world);
            s.Fed = AgentTactics.Fed(me, nowMs);
            s.WantsFood = AgentTactics.WantsFood(a, me, world, nowMs);
            s.Priorities = CurrentPriorities(a, nowMs);
            // bag and equipment, by name
            var counts = new Dictionary<string, int>();
            foreach (var it in me.Inventory.Bag)
            {
                if (it == null)
                    continue;
                var t = world.FindItem(it.Id);
                if (t == null || t.Type == PlayerItems.TypeHp || t.Type == PlayerItems.TypeMp)
                    continue;
                int n;
                counts.TryGetValue(t.Name, out n);
                counts[t.Name] = n + (t.IsUpToUp ? it.Quantity : 1);
            }

            foreach (var kv in counts)
                s.Bag.Add(kv.Key + (kv.Value > 1 ? " x" + kv.Value : ""));
            foreach (var it in me.Inventory.Equipment)
            {
                if (it == null)
                    continue;
                var t = world.FindItem(it.Id);
                if (t != null)
                    s.Equipment.Add(t.Name + (it.Upgrade > 0 ? " +" + it.Upgrade : ""));
            }

            // main quest
            var q = world.FindTask(task);
            if (q != null)
            {
                s.QuestId = task;
                s.QuestName = q.Name;
                s.QuestLevel = q.LevelRequire;
                int idx = me.Progress.TaskIndex;
                if (!me.Progress.TaskActive)
                    s.QuestState = "not taken yet (talk to " + AgentTactics.NpcName(world, PlayerTasks.TaskNpcId(me, world)) + ")";
                else
                {
                    s.QuestState = "step " + (idx + 1) + " of " + q.SubNames.Length;
                    s.QuestStep = idx < q.SubNames.Length ? q.SubNames[idx] : "";
                    int target = idx < q.Counts.Length ? q.Counts[idx] : -1;
                    if (target > 0)
                        s.QuestHint = me.Progress.TaskCount + "/" + target;
                }

                s.QuestGuide = AgentTactics.QuestGuide(me, world);
                s.QuestBlockedByLevel = me.Level < q.LevelRequire && (!me.Progress.TaskActive || idx == 0);
                if (s.QuestBlockedByLevel)
                    s.QuestHint = "needs level " + q.LevelRequire;
                if (me.Record.ClassId == 0 && task >= 9 && !me.Progress.TaskActive)
                    s.QuestHint = me.Level >= 10 ? "join a school next (" + ClassNames[Math.Max(1, Math.Min(6, p.ClassId))] + ")" : "join a school at level 10";
            }

            int tryId;
            long tryPrice; // item ledger: the cheapest village-shop item this character never used
            if (AgentTactics.PickTryItem(a, me, world, nowMs, out tryId, out tryPrice))
            {
                var tt = world.FindItem(tryId);
                s.TryItemId = tryId;
                s.TryItemText = (tt != null ? tt.Name : "item " + tryId) + " (" + (tryPrice == 0 ? "already in the bag" : tryPrice + " yen/xu") + ")";
            }

            s.Memory.AddRange(a.Memory);
            foreach (var f in a.Knowledge.Top(15))
                s.Knowledge.Add(f.Text + (f.Count > 1 ? " (x" + f.Count + ")" : ""));
            foreach (var f in a.Knowledge.NpcMaps())
                if (f.Key != AgentKnowledge.NpcPrefix + me.MapId && s.NpcMemory.Count < AgentConfig.NpcMapsInPrompt)
                    s.NpcMemory.Add(f.Text);
            long now = Now();
            foreach (var it in me.Zone.ItemMaps)
                if (AgentForaging.CanTake(me, world, it, now))
                    s.ItemsNearby++;
            s.Neighbors.AddRange(WorldKnowledge.Neighbors(world, map, task));
            var near = WorldKnowledge.Nearby(world, map, Math.Max(0, AgentConfig.OptionHops), task);
            foreach (var kv in near)
            {
                var tile = world.FindMap(kv.Key);
                if (tile == null)
                    continue;
                s.MapNames[kv.Key] = tile.Name;
                foreach (var m in WorldKnowledge.MobsOf(world, kv.Key))
                {
                    if (m.IsBoss || m.TemplateId == 0 || m.Level > me.Level + 8 || m.Level < me.Level - 12)
                        continue;
                    s.Hunts.Add(new HuntOption { MapId = kv.Key, Hops = kv.Value, MobId = m.TemplateId, MobLevel = m.Level, MapName = tile.Name, MobName = m.Name });
                }
            }

            s.Activity = a.Activity;
            try
            {
                s.MapView = AgentMapView.Build(a, me, world);
                s.Terrain = AgentMapView.BuildTerrain(a, me, world);
            }
            catch (Exception ex)
            {
                s.MapView = "unavailable: " + ex.Message + "\n";
            }

            s.Account = a.Account;
            s.Summary = a.Summary;
            int lv = me.Level;
            s.Hunts.Sort((x, y) => (Math.Abs(x.MobLevel - lv - 1) * 2 + x.Hops).CompareTo(Math.Abs(y.MobLevel - lv - 1) * 2 + y.Hops));
            return s;
        }

        /// <summary>The LLM's latest priorities while they last (AgentConfig.LlmPriorityMinutes), else null (all 1).</summary>
        public static float[] CurrentPriorities(NinjaAgent a, long now)
        {
            if (a.Priorities == null || AgentConfig.LlmPriorityMinutes <= 0 || now - a.PrioritiesAt > AgentConfig.LlmPriorityMinutes * 60000L)
                return null;
            return (float[])a.Priorities.Clone();
        }

        private static bool IsVillage(int map)
        {
            return map == 10 || map == 17 || map == 22 || map == 32 || map == 38 || map == 43 || map == 48 || map == 1 || map == 27 || map == 72;
        }
    }
}
