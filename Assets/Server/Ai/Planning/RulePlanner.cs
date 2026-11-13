using System;
using System.Collections.Generic;

namespace NsoServer.Ai
{
    using Math = System.Math;

    // ====================================================================== fallback: utility AI
    /// <summary>
    /// Rule-based planner (utility AI). Scores rest / hunt / explore / collect / idle from the agent's state and
    /// personality and returns a short plan. Instant and always available: used when the LLM is disabled, busy,
    /// failing or returns an unusable answer.
    /// </summary>
    public sealed class RulePlanner : IStrategicPlanner
    {
        public string Name
        {
            get
            {
                return "rules";
            }
        }

        public Decision Decide(AgentSnapshot s)
        {
            var rng = new Random(s.Seed);
            var d = new Decision
            {
                Source = "rules",
                FleeHpPercent = 20 + (int)(s.Caution * 25)
            };
            // 1) needs: low HP and no potions -> buy some if possible
            bool lowHp = s.HpPercent < 50;
            float shop = s.CanShop && s.CanAffordPotions && s.HpPotions < Math.Max(2, AgentConfig.PotionStock / 2) ? 0.6f + s.Caution * 0.6f + (lowHp ? 0.6f : 0f) : 0f;
            // Player log 2026-11-13: every AI death of the last 36 h was a character that could shop, had over 500 yen
            // and carried no HP potion (the shop score lost to the quest/hunt score). Restocking is mandatory first.
            if (s.CanShop && s.CanAffordPotions && s.HpPotions < AgentConfig.MinPotionsToFight)
                shop = 10f;
            // Food (HP and MP every half second for 30 min) is what players keep running while they train; the shop step
            // buys it after the potions. A short trip worth more than the next quest step.
            bool foodTrip = s.WantsFood && shop < 10f;
            if (foodTrip)
                shop = Math.Max(shop, 2.5f); // above any hunt/quest score (at most about 2)
            // 2) the main quest: it unlocks maps, schools and rewards (players follow it). Play mode 2026-11-13: at 0.9-1.2
            // it lost to the hunt score (up to about 1.75 for aggressive characters) and bots trained beside an open
            // quest step; now it beats any hunt unless the LLM's priorities say otherwise (below the food trip, 2.5).
            float quest = s.QuestId >= 0 && !s.QuestBlockedByLevel ? 1.8f + (1f - s.Curiosity) * 0.3f : 0f;
            // 3) the best hunting ground: monsters near the level, not too far away
            HuntOption best = null;
            float bestScore = 0f;
            foreach (var h in s.Hunts)
            {
                int gap = h.MobLevel - s.Level;
                // too strong / no experience; without potions (before the shop opens) only monsters at most one level above
                int maxGap = s.HpPotions > 0 ? 2 + (int)(s.Aggression * 2) : 1;
                if (gap > maxGap || gap < -9)
                    continue;
                float fit = 1f - Math.Abs(gap - 1) / 10f;
                float score = fit * (0.6f + s.Aggression) - h.Hops * 0.12f * (1.2f - s.Curiosity) + (float)rng.NextDouble() * 0.15f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = h;
                }
            }

            float hunt = best != null ? bestScore + (s.QuestBlockedByLevel ? 0.5f : 0f) : 0f;
            float explore = s.Neighbors.Count > 0 ? s.Curiosity * 0.5f + (float)rng.NextDouble() * 0.2f : 0f;
            float collect = s.ItemsNearby > 0 && s.FreeSlots > 0 ? 0.3f + s.Greed * 0.6f : 0f;
            float idle = s.InVillage ? s.Sociability * 0.4f * (s.PlayersHere > 0 ? 1.5f : 0.6f) : 0.05f;
            // User 2026-11-13: a curious player buys an unknown item to see what it does (item ledger). Below the quest
            // (1.8+) so it happens between quest steps or while training; the LLM's shop weight scales it.
            float tryItem = s.TryItemId >= 0 ? Math.Max(0.05f, 0.3f + s.Curiosity * 0.9f - s.Greed * 0.5f) : 0f; // thrifty: tries less
            // the LLM's priorities for this situation (Priority; null = all 1); the mandatory potion restock stays
            var w = s.Priorities;
            if (w != null)
            {
                quest *= w[Priority.Quest];
                hunt *= w[Priority.Hunt];
                explore *= w[Priority.Explore];
                collect *= w[Priority.Collect];
                idle = Math.Max(0.01f, idle * w[Priority.Idle]);
                if (shop < 10f)
                    shop *= w[Priority.Shop];
                tryItem *= w[Priority.Shop];
            }

            float top = Math.Max(Math.Max(Math.Max(shop, quest), Math.Max(hunt, explore)), Math.Max(Math.Max(collect, idle), tryItem));
            if (top == shop && shop > 0)
            {
                d.Reason = foodTrip && s.HpPotions >= Math.Max(2, AgentConfig.PotionStock / 2) ? "No food left: buy some at Tabemono, it restores HP and MP for 30 minutes." : "Only " + s.HpPotions + " HP potions left, buy more (and food) before fighting.";
                d.Steps.Add(new PlanStep { Goal = GoalType.Shop });
                d.Steps.Add(new PlanStep { Goal = s.QuestId >= 0 && !s.QuestBlockedByLevel ? GoalType.Quest : GoalType.Hunt, MapId = best != null && s.QuestBlockedByLevel ? best.MapId : -1, MobId = best != null && s.QuestBlockedByLevel ? best.MobId : -1, Count = 10 });
            }
            else if (top == tryItem && tryItem > 0)
            {
                d.Reason = "Never used " + s.TryItemText + ": buy one and try what it does.";
                d.Steps.Add(new PlanStep { Goal = GoalType.Shop, MobId = s.TryItemId, Count = 1 });
                if (s.QuestId >= 0 && !s.QuestBlockedByLevel)
                    d.Steps.Add(new PlanStep { Goal = GoalType.Quest });
            }
            else if (top == quest && quest > 0)
            {
                d.Reason = "Continue the main quest " + s.QuestName + ".";
                d.Steps.Add(new PlanStep { Goal = GoalType.Quest });
            }
            else if (top == collect && collect > 0)
            {
                d.Reason = "There are items lying nearby.";
                d.Steps.Add(new PlanStep { Goal = GoalType.Collect, Count = Math.Min(5, s.ItemsNearby) });
            }
            else if (top == hunt && best != null)
            {
                d.Reason = (s.QuestBlockedByLevel ? "Need level " + s.QuestLevel + " for the next quest: " : "") + "train on " + best.MobName + " (level " + best.MobLevel + ") in " + best.MapName + ".";
                d.Steps.Add(Hunt(best, s));
                if (s.Greed > 0.5f)
                    d.Steps.Add(new PlanStep { Goal = GoalType.Collect, Count = 3 });
            }
            else if (top == explore && s.Neighbors.Count > 0)
            {
                int map = s.Neighbors[rng.Next(s.Neighbors.Count)];
                string name;
                s.MapNames.TryGetValue(map, out name);
                d.Reason = "Curious about " + (name ?? "map " + map) + ".";
                d.Steps.Add(new PlanStep { Goal = GoalType.Explore, MapId = map });
            }
            else
            {
                d.Reason = s.InVillage ? "Hang around the village for a while." : "Nothing better to do, wait a little.";
                d.Steps.Add(new PlanStep { Goal = GoalType.Idle, Seconds = 20 + rng.Next(30) });
            }

            return d;
        }

        private static PlanStep Hunt(HuntOption h, AgentSnapshot s)
        {
            return new PlanStep
            {
                Goal = GoalType.Hunt,
                MapId = h.MapId,
                MobId = h.MobId,
                Count = 5 + (int)(s.Aggression * 10)
            };
        }
    }
}
