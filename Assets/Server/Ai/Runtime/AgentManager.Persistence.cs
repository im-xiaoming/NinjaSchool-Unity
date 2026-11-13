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
        // ================================================================== persistence (the AI's mind; the characters are saved as players)
        private static Dictionary<string, Dictionary<string, object>> Load()
        {
            var map = new Dictionary<string, Dictionary<string, object>>();
            try
            {
                string text = server.Accounts.GetDocument(SaveKey);
                if (text == null)
                    return map;
                var list = Json.Parse(text) as List<object>;
                if (list == null)
                    return map;
                foreach (var o in list)
                {
                    var d = o as Dictionary<string, object>;
                    object name;
                    if (d != null && d.TryGetValue("name", out name) && name is string)
                        map[(string)name] = d;
                }
            }
            catch (Exception ex)
            {
                Log("AI memory load error (fresh minds): " + ex.Message);
            }

            return map;
        }

        private static void Restore(NinjaAgent a, Dictionary<string, object> d)
        {
            object o;
            if (d.TryGetValue("kills", out o))
                a.Kills = (int)Json.Long(o);
            if (d.TryGetValue("deaths", out o))
                a.Deaths = (int)Json.Long(o);
            if (d.TryGetValue("quests", out o))
                a.QuestsDone = (int)Json.Long(o);
            if (d.TryGetValue("memory", out o) && o is List<object> mem)
                foreach (var m in mem)
                    if (m is string)
                        a.Memory.Add((string)m);
            if (d.TryGetValue("knowledge", out o))
                a.Knowledge.FromJson(o);
            if (d.TryGetValue("summary", out o) && o is string)
                a.Summary = (string)o;
            if (d.TryGetValue("rewardHistory", out o))
                a.Learning.ItemRewards.Restore(o);
        }

        private static void Save()
        {
            if (server == null)
                return;
            var w = new JsonWriter();
            w.BeginArray();
            foreach (var a in Agents)
            {
                w.BeginObject();
                w.Prop("name", a.Name);
                w.Prop("kills", a.Kills);
                w.Prop("deaths", a.Deaths);
                w.Prop("quests", a.QuestsDone);
                w.Key("memory");
                w.Value(new List<string>(a.Memory));
                w.Key("knowledge");
                w.Value(a.Knowledge.ToJson());
                w.Prop("summary", a.Summary);
                w.Key("rewardHistory");
                w.Value(a.Learning.ItemRewards.ToObject());
                w.EndObject();
            }

            w.EndArray();
            server.Accounts.SetDocument(SaveKey, w.ToString());
            Rl.RlRuntime.SaveHumanRewardHistories();
        }
    }
}
