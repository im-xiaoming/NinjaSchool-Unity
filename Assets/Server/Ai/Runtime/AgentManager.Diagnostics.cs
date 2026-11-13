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
        /// <summary>Forces a new decision now (dev terminal /replan of the human player). Null or "" = every AI player.</summary>
        public static int ForceReplan(string name)
        {
            int n = 0;
            if (world == null)
                return 0;
            foreach (var a in Agents)
            {
                if (!string.IsNullOrEmpty(name) && !a.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (a.Me == null || a.Me.IsDead || a.Pending)
                    continue;
                a.Trigger = "asked to reconsider the plan";
                a.LastDecisionAt = 0;
                a.NeedsDecision = true;
                n++;
            }

            return n;
        }

        /// <summary>One line per AI player and one for the LLM (dev terminal /agents).</summary>
        public static List<string> Summary()
        {
            var lines = new List<string>();
            if (world == null)
            {
                lines.Add("AI players are off (NSO Server Control > AI players).");
                return lines;
            }

            foreach (var a in Agents)
            {
                var me = a.Me;
                if (me == null)
                {
                    lines.Add(a.Name + ": " + a.LoginStatus);
                    continue;
                }

                lines.Add(a.Name + " lv" + me.Level + " map " + me.MapId + " quest " + me.Record.TaskId + " HP " + AgentTactics.HpPercent(me) + "% - " + a.Activity + (a.Plan != null ? " | [" + a.Plan.Source + "] " + (a.Step != null ? a.Step.ToString() : "") : "") + (a.Pending ? " | waiting LLM" : ""));
            }

            lines.Add(LlmStatus(Now()));
            return lines;
        }

        private static string LlmStatus(long now)
        {
            if (service == null)
                return "LLM: off (rules only)";
            return "LLM " + AgentConfig.LlmModel + ": " + (service.IsAvailable(now) ? "ok" : "backing off") + ", " + service.Successes + "/" + service.Requests + " answered, last " + service.LastLatencyMs + " ms" + (service.LastError.Length > 0 ? ", last error " + service.LastError : "");
        }

        /// <summary>Full debug state (wiki route /control/agents). Call under SyncRoot.</summary>
        public static Dictionary<string, object> DebugObject()
        {
            long now = Now();
            var root = new Dictionary<string, object>();
            root["running"] = world != null;
            root["brain"] = new Dictionary<string, object>
            {
                {
                    "endpoint",
                    AgentConfig.BrainEndpoint
                },
                {
                    "autoStart",
                    AgentConfig.BrainAutoStart
                },
                {
                    "error",
                    BrainClient.LastError
                }
            };
            root["runtimeConfigError"] = NsoServer.Configuration.RuntimeConfiguration.LastError;
            root["learning"] = Rl.RlRuntime.Debug();
            var llm = new Dictionary<string, object>();
            llm["enabled"] = AgentConfig.LlmEnabled;
            llm["endpoint"] = AgentConfig.LlmEndpoint;
            llm["model"] = AgentConfig.LlmModel;
            llm["status"] = LlmStatus(now);
            if (service != null)
            {
                llm["available"] = service.IsAvailable(now);
                llm["busy"] = service.Busy;
                llm["queue"] = service.QueueLength;
                llm["requests"] = service.Requests;
                llm["successes"] = service.Successes;
                llm["failures"] = service.Failures;
                llm["lastLatencyMs"] = service.LastLatencyMs;
                llm["lastError"] = service.LastError;
                llm["lastAnswer"] = service.LastAnswer;
                llm["lastFailedAnswer"] = service.LastFailedAnswer;
                llm["lastFailedAt"] = service.LastFailedAt;
            }

            root["llm"] = llm;
            // long-term memory: the whole game knowledge (GameKnowledge) plus what the AI players learn while playing
            root["rag"] = new Dictionary<string, object>
            {
                {
                    "enabled",
                    AgentConfig.RagEnabled
                },
                {
                    "available",
                    AgentRag.Available
                },
                {
                    "stored",
                    System.Threading.Interlocked.Read(ref AgentRag.Stored)
                },
                {
                    "recalls",
                    System.Threading.Interlocked.Read(ref AgentRag.Recalls)
                },
                {
                    "knowledge",
                    GameKnowledge.Documents
                },
                {
                    "knowledgeError",
                    GameKnowledge.LastError
                },
                {
                    "error",
                    AgentRag.LastError
                }
            };
            var list = new List<object>();
            foreach (var a in Agents)
            {
                var d = new Dictionary<string, object>();
                var me = a.Me;
                d["id"] = a.Index;
                d["name"] = a.Name;
                d["account"] = a.Account;
                d["traits"] = a.P.Traits();
                d["bio"] = a.P.Bio;
                d["online"] = me != null;
                d["loginStatus"] = a.LoginStatus;
                d["kills"] = a.Kills;
                d["deaths"] = a.Deaths;
                d["questsDone"] = a.QuestsDone;
                d["learning"] = new Dictionary<string, object>
                {
                    {
                        "source",
                        a.Learning.LastSource
                    },
                    {
                        "fallback",
                        a.Learning.Fallback
                    },
                    {
                        "version",
                        a.PolicyVersion
                    },
                    {
                        "reward",
                        a.Learning.Reward
                    },
                    {
                        "stuck",
                        a.Learning.Stuck
                    },
                    {
                        "actions",
                        new Dictionary<string, int>(a.Learning.Actions)
                    }
                };
                d["actionRewards"] = a.Learning.ItemRewards.Debug();
                var ledger = new List<object>(); // item ledger: what this character learned each item does
                foreach (var kv in a.Learning.ItemRewards.Ledger)
                {
                    var t = server != null && server.World != null ? server.World.FindItem(kv.Key) : null;
                    ledger.Add(kv.Value.Text(t != null ? t.Name : "item " + kv.Key));
                }

                d["itemLedger"] = ledger;
                d["activity"] = a.Activity;
                d["recovering"] = a.Recovering;
                d["pending"] = a.Pending;
                d["needsDecision"] = a.NeedsDecision;
                d["trigger"] = a.Trigger;
                d["nextReviewInS"] = Math.Max(0, (a.NextReviewAt - now) / 1000);
                if (me != null)
                {
                    long expLevel = world.ExpForLevel(me.Level), expNext = world.ExpForLevel(me.Level + 1);
                    d["charId"] = me.Record.Id;
                    d["level"] = me.Level;
                    d["exp"] = me.Record.Exp;
                    d["expPercent"] = expNext > expLevel ? Math.Round(Math.Max(0, Math.Min(100, (me.Record.Exp - expLevel) * 100.0 / (expNext - expLevel))), 1) : 100.0;
                    d["class"] = me.Record.ClassId;
                    d["className"] = me.Record.ClassId >= 0 && me.Record.ClassId < ClassNames.Length ? ClassNames[me.Record.ClassId] : "";
                    d["gender"] = me.Record.Gender;
                    d["hp"] = me.Hp;
                    d["maxHp"] = me.MaxHp;
                    d["mp"] = me.Mp;
                    d["maxMp"] = me.MaxMp;
                    d["yen"] = me.Progress.Yen;
                    d["xu"] = me.Progress.Xu;
                    d["dead"] = me.IsDead;
                    d["map"] = me.MapId;
                    d["mapName"] = me.Zone.Tile.Name;
                    d["zone"] = me.Zone.Id;
                    d["x"] = (int)me.X;
                    d["y"] = (int)me.Y;
                    d["withHumans"] = me.Zone.Players.Exists(o =>
                    {
                        var p = (PlayerState)o;
                        return p.Session != null && !p.Session.IsBot;
                    });
                    d["hpPotions"] = AgentTactics.HpPotions(me, world);
                    d["mpPotions"] = AgentTactics.MpPotions(me, world);
                    d["foods"] = AgentTactics.Foods(me, world);
                    // consumables: the last one used and the effects running now (potion regeneration, food...)
                    if (a.Client != null && a.Client.LastUsedItemId >= 0)
                        d["lastItem"] = new Dictionary<string, object>
                        {
                            {
                                "id",
                                a.Client.LastUsedItemId
                            },
                            {
                                "name",
                                a.Client.LastUsedItemName
                            },
                            {
                                "secondsAgo",
                                Math.Max(0, (now - a.Client.LastUsedItemAt) / 1000)
                            },
                            {
                                "count",
                                a.Client.UsedItems
                            }
                        };
                    var effects = new List<object>();
                    foreach (var e in me.Effects)
                        if (!e.IsExpired(now))
                            effects.Add(new Dictionary<string, object> { { "id", e.TemplateId }, { "name", world.EffectName(e.TemplateId) }, { "type", e.Type }, { "seconds", Math.Max(0, (e.EndAt - now + 999) / 1000) } });
                    d["effects"] = effects;
                    d["freeSlots"] = PlayerItems.SlotNull(me);
                    d["bagSize"] = me.Inventory.Bag.Length;
                    var bag = new List<object>();
                    foreach (var it in me.Inventory.Bag)
                    {
                        if (it == null)
                            continue;
                        var t = world.FindItem(it.Id);
                        bag.Add((t != null ? t.Name : "#" + it.Id) + (it.Quantity > 1 ? " x" + it.Quantity : ""));
                    }

                    d["bag"] = bag;
                    var eq = new List<object>();
                    foreach (var it in me.Inventory.Equipment)
                    {
                        if (it == null)
                            continue;
                        var t = world.FindItem(it.Id);
                        eq.Add((t != null ? t.Name : "#" + it.Id) + (it.Upgrade > 0 ? " +" + it.Upgrade : ""));
                    }

                    d["equipment"] = eq;
                    var q = world.FindTask(me.Record.TaskId);
                    var quest = new Dictionary<string, object>();
                    quest["id"] = me.Record.TaskId;
                    quest["name"] = q != null ? q.Name : "(all done)";
                    quest["active"] = me.Progress.TaskActive;
                    quest["index"] = me.Progress.TaskIndex;
                    quest["steps"] = q != null ? q.SubNames.Length : 0;
                    quest["step"] = q != null && me.Progress.TaskActive && me.Progress.TaskIndex < q.SubNames.Length ? q.SubNames[me.Progress.TaskIndex] : "";
                    quest["count"] = me.Progress.TaskCount;
                    quest["target"] = q != null && me.Progress.TaskActive && me.Progress.TaskIndex < q.Counts.Length ? (int)q.Counts[me.Progress.TaskIndex] : -1;
                    quest["unlockedMaps"] = MapUnlock.Allowed(me.Record.TaskId).Count;
                    d["quest"] = quest;
                }

                if (a.Plan != null)
                {
                    var p = new Dictionary<string, object>();
                    p["source"] = a.Plan.Source;
                    p["reason"] = a.Plan.Reason;
                    p["say"] = a.Plan.Say;
                    p["fleeHp"] = a.FleeHp;
                    var steps = new List<object>();
                    foreach (var s in a.Plan.Steps)
                        steps.Add(s + (s.Note.Length > 0 ? " [" + s.Note + "]" : ""));
                    p["steps"] = steps;
                    p["stepIndex"] = a.StepIndex;
                    p["stepProgress"] = a.StepProgress;
                    p["stepSeconds"] = (now - a.StepStartedAt) / 1000;
                    d["plan"] = p;
                }

                var pw = CurrentPriorities(a, now);
                if (pw != null)
                {
                    var pr = new Dictionary<string, object>();
                    for (int i = 0; i < pw.Length; i++)
                        pr[Priority.Names[i]] = Math.Round(pw[i], 2);
                    d["priorities"] = pr;
                    d["prioritiesAgeS"] = (now - a.PrioritiesAt) / 1000;
                }

                d["memory"] = new List<object>(a.Memory.ToArray());
                var know = new List<object>();
                foreach (var f in a.Knowledge.All())
                    know.Add(new Dictionary<string, object> { { "key", f.Key }, { "text", f.Text }, { "count", f.Count } });
                d["knowledge"] = know;
                d["decisions"] = new List<object>(a.Decisions.ToArray());
                list.Add(d);
            }

            root["agents"] = list;
            return root;
        }
    }
}
