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
        // event-driven requests, throttled; a plan under review keeps running until the answer comes
        private static void MaybeDecide(NinjaAgent a, long now)
        {
            var me = a.Me;
            if (me == null || me.IsDead)
                return;
            // LLM advice is asynchronous and must never stop the RL/teacher decision loop.
            if (a.Pending && now - a.PendingSince > (AgentConfig.LlmTimeoutSeconds + 15) * 1000L)
                a.Pending = false;
            bool throttled = now - a.LastDecisionAt < AgentConfig.MinDecisionSeconds * 1000L;
            if (a.NeedsDecision)
            {
                // A player with no plan gets a teacher/RL decision immediately, independently of LLM availability.
                if (!throttled || a.Plan == null)
                    Decide(a, now, null, false);
                return;
            }

            // Busy for a long time without any result (no kill, pickup, quest step, new map...): the LLM steps in, told what
            // is going on (with the map coordinates), and its plan replaces the stuck one (AgentConfig.LlmStallSeconds).
            if (AgentConfig.LlmStallSeconds > 0 && service != null && AgentConfig.LlmEnabled && service.IsAvailable(now) && !a.Pending && a.Plan != null && now - a.LastProgressAt > AgentConfig.LlmStallSeconds * 1000L && now - a.LastStallAskAt > AgentConfig.LlmStallSeconds * 1000L)
            {
                a.LastStallAskAt = now;
                long idle = (now - a.LastProgressAt) / 1000;
                a.Trigger = "NO RESULT for " + idle + " s while \"" + a.Activity + "\" (step " + (a.Step != null ? a.Step.ToString() : "none") + "). The current approach is not working: choose a different one (another target, place, map or goal; goto coordinates from CURRENT MAP).";
                Remember(a, "stuck: no result for " + idle + " s while " + a.Activity);
                Decide(a, now, null, false);
                return;
            }

            if (AgentConfig.ReviewSeconds > 0 && now >= a.NextReviewAt && !throttled)
            {
                a.Trigger = "periodic review of the current plan (" + (a.Step != null ? a.Step.ToString() : "none") + ")";
                Decide(a, now, null, false);
            }
        }

        private static void Decide(NinjaAgent a, long now, string fallbackWhy, bool forceRules)
        {
            a.NeedsDecision = false;
            a.LastDecisionAt = now;
            int review = AgentConfig.LlmLeads && service != null ? AgentConfig.LlmReviewSeconds : AgentConfig.ReviewSeconds;
            a.NextReviewAt = now + Math.Max(30, review) * 1000L;
            var snap = Snapshot(a);
            snap.RequestSequence = a.DecisionRequest + 1;
            if (!a.Pending && !forceRules && service != null && AgentConfig.LlmEnabled && service.IsAvailable(now) && service.Enqueue(snap))
            {
                a.DecisionRequest = snap.RequestSequence;
                a.Pending = true;
                a.PendingSince = now;
            }

            // LLM leads: a plan still running is kept until the LLM answers; rules/RL only fill a gap (no plan, or the
            // LLM is unavailable / failed). Otherwise rules/RL decide at once and the LLM only advises.
            if (AgentConfig.LlmLeads && a.Pending && a.Plan != null && a.Step != null)
                return;
            Apply(a, Rl.RlRuntime.Decide(a, world, snap, Rules.Decide(snap), now), now, fallbackWhy);
        }

        private static void DrainResults(long now)
        {
            if (service == null)
                return;
            DecisionResult r;
            while (service.TryTake(out r))
            {
                NinjaAgent a = null;
                foreach (var x in Agents)
                    if (x.Index == r.AgentId)
                        a = x;
                if (a == null || !a.Pending || r.RequestSequence != a.DecisionRequest)
                    continue; // timed out meanwhile: rules already decided
                a.Pending = false;
                if (a.Me == null || a.Me.IsDead)
                {
                    a.NeedsDecision = true;
                    continue;
                }

                // LLM leads (AgentConfig.LlmLeads): its validated plan replaces the interim rules/RL plan. Otherwise it only
                // suggests goals/risk/speech (RL features) and RL or its teacher installs plans.
                if (r.Decision != null)
                {
                    a.Learning.Advice = r.Decision;
                    if (r.Decision.Summary != null)
                    {
                        // the context was full: the LLM's summary replaces the oldest events (they stay in the vector memory)
                        a.Summary = r.Decision.Summary;
                        int cut = Math.Min(r.Decision.SummaryCut, a.Memory.Count);
                        if (cut > 0)
                            a.Memory.RemoveRange(0, cut);
                        PlayerLog.Bot(a.Account, "AI", "summary: " + a.Summary);
                    }

                    if (AgentConfig.LlmLeads && r.Decision.Steps.Count > 0 && Apply(a, r.Decision, now, null))
                    {
                    }
                    else if (!string.IsNullOrEmpty(r.Decision.Say))
                        Say(a, r.Decision.Say);
                }
                else
                    a.Remember("LLM advice unavailable: " + r.Error);
            }
        }

        /// <summary>Validates a decision against what the character can do and installs it as the plan. Locked or
        /// unreachable maps and monsters that do not live there are corrected or dropped (noted on the step).</summary>
        private static bool Apply(NinjaAgent a, Decision d, long now, string fallbackWhy)
        {
            var me = a.Me;
            if (me == null)
                return false;
            int task = me.Record.TaskId;
            var v = new Decision
            {
                Reason = d.Reason ?? "",
                Say = d.Say ?? "",
                Source = d.Source,
                Raw = d.Raw,
                FleeHpPercent = d.FleeHpPercent < 0 ? -1 : Math.Max(10, Math.Min(60, d.FleeHpPercent))
            };
            // The LLM re-weights the rule planner on every answer (dynamic, by context); the rules use the weights for
            // the decisions they make while the LLM is busy.
            if (d.Priorities != null && d.Priorities.Length == Priority.Names.Length && AgentConfig.LlmPriorityMinutes > 0)
            {
                var w = new float[Priority.Names.Length];
                for (int i = 0; i < w.Length; i++)
                    w[i] = float.IsNaN(d.Priorities[i]) ? 1f : Math.Max(0f, Math.Min(Priority.Max, d.Priorities[i]));
                bool changed = a.Priorities == null;
                for (int i = 0; i < w.Length && !changed; i++)
                    changed = Math.Abs(a.Priorities[i] - w[i]) > 0.01f;
                a.Priorities = w;
                a.PrioritiesAt = now;
                if (changed)
                    PlayerLog.Bot(a.Account, "AI", "priorities: " + Priority.Text(w));
            }

            int map = me.MapId; // where each step starts
            foreach (var st in d.Steps)
            {
                if (v.Steps.Count >= 4)
                    break;
                var s = new PlanStep
                {
                    Goal = st.Goal,
                    MapId = st.MapId,
                    MobId = st.MobId,
                    Count = st.Count,
                    Seconds = st.Seconds,
                    X = st.X,
                    Y = st.Y
                };
                if (s.Goal == GoalType.Move && s.X < 0)
                    continue; // goto without coordinates
                if ((s.Goal == GoalType.Hunt || s.Goal == GoalType.Explore || s.Goal == GoalType.Travel || s.Goal == GoalType.Move) && s.MapId >= 0 && (!WorldKnowledge.IsWalkable(world, s.MapId) || WorldKnowledge.Route(world, map, s.MapId, task) == null))
                {
                    s.Note = MapUnlock.CanEnter(task, s.MapId) ? "map " + s.MapId + " is not reachable" : "map " + s.MapId + " is locked (main quest)";
                    if (s.Goal == GoalType.Travel || s.Goal == GoalType.Move)
                        continue;
                    s.MapId = -1;
                }

                if (s.Goal == GoalType.Hunt)
                {
                    int where = s.MapId >= 0 ? s.MapId : map;
                    if (s.MobId >= 0 && !WorldKnowledge.MobsOf(world, where).Exists(k => k.TemplateId == s.MobId))
                    {
                        s.Note = "monster " + s.MobId + " does not live in map " + where;
                        s.MobId = -1;
                    }

                    s.Count = Math.Max(1, Math.Min(30, s.Count));
                }

                // shop with an item id = buy one and try it (AgentTactics.TryItem): only items of the village shops
                if (s.Goal == GoalType.Shop && s.MobId >= 0 && AgentTactics.SoldBy(world, s.MobId) < 0)
                {
                    s.Note = "item " + s.MobId + " is not sold in the village shops";
                    s.MobId = -1;
                }

                if (s.Goal == GoalType.Collect)
                    s.Count = Math.Max(1, Math.Min(10, s.Count));
                if (s.Goal == GoalType.Idle)
                    s.Seconds = Math.Max(5, Math.Min(120, s.Seconds));
                if (s.MapId >= 0)
                    map = s.MapId;
                v.Steps.Add(s);
            }

            if (v.Steps.Count == 0)
                return false;
            a.Plan = v;
            a.StepIndex = 0;
            a.PlanStartedAt = now;
            BeginStep(a, now);
            var line = new StringBuilder();
            line.Append(DateTime.Now.ToString("HH:mm:ss")).Append(" [").Append(v.Source).Append("] ").Append(v.Reason);
            if (fallbackWhy != null)
                line.Append(" (fallback: ").Append(fallbackWhy).Append(')');
            line.Append(" -> ");
            for (int i = 0; i < v.Steps.Count; i++)
                line.Append(i > 0 ? "; " : "").Append(v.Steps[i]).Append(v.Steps[i].Note.Length > 0 ? " [" + v.Steps[i].Note + "]" : "");
            a.Decisions.Add(line.ToString());
            while (a.Decisions.Count > 10)
                a.Decisions.RemoveAt(0);
            a.Remember("decided: " + v.Reason);
            PlayerLog.Bot(a.Account, "AI", line.ToString());
            if (!string.IsNullOrEmpty(v.Say))
                Say(a, v.Say);
            return true;
        }

        private static void BeginStep(NinjaAgent a, long now)
        {
            a.StepStartedAt = now;
            a.LastProgressAt = now;
            a.StepProgress = 0;
            a.TargetMob = -1;
            a.TargetItem = -1;
            a.LastTargetSeen = 0;
            a.Route = null;
            a.WanderX = -1;
            if (a.Me != null)
                a.LastTaskId = a.Me.Record.TaskId;
        }

        /// <summary>Tactical -> strategic: the current step is complete; the plan moves on or is done.</summary>
        public static void StepDone(NinjaAgent a, GameWorld w, long now)
        {
            if (a.Plan == null)
                return;
            a.StepIndex++;
            BeginStep(a, now);
            if (a.StepIndex >= a.Plan.Steps.Count)
            {
                string reason = a.Plan.Reason;
                a.Plan = null;
                Report(a, AgentEvent.PlanDone, "completed the plan: " + reason);
            }
        }

        /// <summary>Tactical -> strategic: the current step cannot be done; the plan is dropped and a new decision is due.</summary>
        public static void StepFailed(NinjaAgent a, GameWorld w, long now, string why)
        {
            a.Plan = null;
            BeginStep(a, now);
            Report(a, AgentEvent.StepFailed, "failed: " + why);
        }
    }
}
