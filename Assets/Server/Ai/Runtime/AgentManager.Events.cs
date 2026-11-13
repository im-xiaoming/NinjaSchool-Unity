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
        /// <summary>An event from the tactical layer: remembered, logged, and for important ones a new decision is due.</summary>
        public static void Report(NinjaAgent a, AgentEvent e, string text)
        {
            Remember(a, text);
            switch (e)
            {
                case AgentEvent.PlanDone:
                case AgentEvent.StepFailed:
                case AgentEvent.Respawned:
                case AgentEvent.LevelUp:
                case AgentEvent.Spawned:
                case AgentEvent.PlayerNearby:
                case AgentEvent.Quest:
                    a.NeedsDecision = true;
                    a.Trigger = text;
                    break;
                case AgentEvent.Died:
                    a.Plan = null;
                    a.Trigger = text;
                    break;
            }
        }

        /// <summary>Remembers an event (short memory) and writes it to the bot log.</summary>
        public static void Remember(NinjaAgent a, string text)
        {
            a.Remember(text);
            PlayerLog.Bot(a.Account, "EVT", text);
        }

        // ================================================================== what happened (knowledge)
        /// <summary>The character died: remember who did it (the monster that was fighting it) and where.</summary>
        public static void OnDied(NinjaAgent a, GameWorld w)
        {
            var me = a.Me;
            a.Deaths++;
            a.Recovering = false;
            Mob killer = null;
            foreach (var mob in me.Zone.Mobs)
                if (!mob.IsDead && mob.Chars.Contains(me.Record.Id) && (killer == null || Math.Abs(mob.X - me.X) < Math.Abs(killer.X - me.X)))
                    killer = mob;
            string who = killer != null ? killer.Template.Name + " (lv " + killer.Level + ")" : "monsters";
            Report(a, AgentEvent.Died, "was killed by " + who + " on map " + me.MapId + " " + me.Zone.Tile.Name + " at level " + me.Level + " (HP potions left: " + AgentTactics.HpPotions(me, w) + ")");
            if (killer != null)
                a.Knowledge.Learn("danger:" + killer.Template.Id, killer.Template.Name + " lv " + killer.Level + " on map " + me.MapId + " " + me.Zone.Tile.Name + " killed me (last time at my level " + me.Level + ")");
            if (a.P.Sociability >= 0.6f)
                Say(a, "Ui da...");
        }

        /// <summary>A monster the character was fighting died: hunting-ground statistics.</summary>
        public static void OnKill(NinjaAgent a, GameWorld w, Mob mob)
        {
            var me = a.Me;
            string key = "ground:" + me.MapId + ":" + mob.Template.Id;
            var f = a.Knowledge.Get(key);
            int n = f != null ? f.Count + 1 : 1;
            a.Knowledge.Learn(key, "hunted " + mob.Template.Name + " lv " + mob.Level + " on map " + me.MapId + " " + me.Zone.Tile.Name + ": " + n + " kills, last at my level " + me.Level);
            if (n % 10 == 0)
                Report(a, AgentEvent.Kill, "defeated " + n + " " + mob.Template.Name + " on map " + me.MapId + " so far");
        }

        /// <summary>The character picked an item up: remember finds that are not money.</summary>
        public static void OnPicked(NinjaAgent a, GameWorld w, ItemTemplateData t, int quantity)
        {
            if (t.Type == PlayerItems.TypeMoney)
            {
                if (quantity >= 200)
                    Remember(a, "picked up " + quantity + " yen");
                return;
            }

            Remember(a, "picked up " + t.Name + (quantity > 1 ? " x" + quantity : ""));
            a.Knowledge.Learn("item:" + t.Id, "found " + t.Name + " (level " + t.Level + ") on map " + a.Me.MapId);
        }

        /// <summary>A quest menu click changed the quest state (taken, a talk step done, reported).</summary>
        public static void OnQuestProgress(NinjaAgent a, GameWorld w, string entry, string npc)
        {
            var me = a.Me;
            int task = me.Record.TaskId;
            if (task != a.LastTaskId)
            {
                var done = w.FindTask(a.LastTaskId);
                a.QuestsDone++;
                a.Knowledge.Learn("quest:" + a.LastTaskId, "finished quest " + (done != null ? done.Name : a.LastTaskId.ToString()) + " at level " + me.Level);
                var unlocked = new List<int>(MapUnlock.Allowed(task));
                unlocked.RemoveAll(m => MapUnlock.Allowed(a.LastTaskId).Contains(m));
                string maps = "";
                foreach (int m in unlocked)
                {
                    var tile = w.FindMap(m);
                    maps += (maps.Length > 0 ? ", " : "") + m + (tile != null ? " " + tile.Name : "");
                }

                if (maps.Length > 0)
                    a.Knowledge.Learn("note:unlocked" + task, "quest " + a.LastTaskId + " unlocked maps " + maps);
                Report(a, AgentEvent.Quest, "finished quest " + (done != null ? done.Name : "") + " with " + npc + (maps.Length > 0 ? "; new maps: " + maps : ""));
                a.LastTaskId = task;
                if (a.P.Sociability >= 0.5f)
                    Say(a, "Xong nhiệm vụ rồi!");
            }
            else
                Remember(a, "quest: \"" + entry + "\" with " + npc);
        }

        // ================================================================== speech
        /// <summary>MapService.chat (CHAT_MAP): a speech bubble over the character for the players of its zone.</summary>
        public static void Say(NinjaAgent a, string text)
        {
            var me = a.Me;
            if (me == null || string.IsNullOrEmpty(text))
                return;
            Presence.Say(me, text);
            PlayerLog.Bot(a.Account, "SAY", text);
        }
    }
}
