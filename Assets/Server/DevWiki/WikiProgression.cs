using System;
using System.Collections.Generic;
using NsoServer.Data;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.DevWiki
{
    public sealed partial class WikiControl
    {
        private GameBalanceRules CloneBalance()
        {
            var writer = new JsonWriter();
            writer.Value(server.World.BalanceRules.ToObject());
            return GameBalanceRules.FromObject(Object(Json.Parse(writer.ToString())), server.World);
        }

        private void SetProgression(GameBalanceRules balance, Dictionary<string, object> request, Dictionary<string, object> values, bool reset)
        {
            object raw;
            string scope = request.TryGetValue("scope", out raw) ? raw as string : null;
            if (!CharacterProgressionRules.ValidScope(scope))
                throw new ArgumentException("Choose the unclassed, classed or class-specific profile.");
            if (reset)
                balance.CharacterProgression.Profiles.Remove(scope);
            else
            {
                var profile = CharacterProgressionRules.ParseProfile(values);
                if (profile.Count == 0)
                    balance.CharacterProgression.Profiles.Remove(scope);
                else
                    balance.CharacterProgression.Profiles[scope] = profile;
            }
        }

        private object ProgressionMetadata()
        {
            var defaults = new Dictionary<string, object>();
            var classIds = new List<int>();
            var original = new GameBalanceRules();
            for (int cls = 0; cls <= CharacterProgressionRules.MaxClassId; cls++)
            {
                classIds.Add(cls);
                var sample = PlayerCombat.ProgressionSample(server.World, original, cls, 1);
                defaults[cls.ToString()] = new Dictionary<string, object>
                {
                    {
                        "base",
                        PlayerTuning.Values(sample, server.World, original)
                    },
                    {
                        "potentialGrowth",
                        CharacterProgressionRules.OriginalGrowth(cls)
                    }
                };
            }

            return Obj("defaults", defaults, "maxLevel", System.Math.Min(200, server.World.MaxLevel), "classIds", classIds, "woodenSwordItemId", PlayerItems.CreateTaskWoodenSword().Id, "profiles", server.World.BalanceRules.CharacterProgression.ToObject(), "formula", "Base + native growth * growthPercent / 100 + (level - 1) * growthFlat + allocated/equipment/skill contributions");
        }

        private KeyValuePair<int, string> PreviewProgression(Dictionary<string, object> request, Dictionary<string, object> values, int expected)
        {
            if (!request.ContainsKey("from") || !request.ContainsKey("to") || !request.ContainsKey("classes"))
                throw new ArgumentException("Choose level range and comparison classes.");
            int max = System.Math.Min(200, server.World.MaxLevel);
            int from = GameBalanceRules.Integer(request["from"], 1, max), to = GameBalanceRules.Integer(request["to"], from, max);
            var classes = request["classes"] as List<object>;
            if (classes == null || classes.Count < 1 || classes.Count > CharacterProgressionRules.MaxClassId + 1)
                throw new ArgumentException("Choose distinct supported classes.");
            var ids = new HashSet<int>();
            foreach (var raw in classes)
                if (!ids.Add(GameBalanceRules.Integer(raw, 0, CharacterProgressionRules.MaxClassId)))
                    throw new ArgumentException("Duplicate preview class.");
            if (request.ContainsKey("includeWoodenSword") && !(request["includeWoodenSword"] is bool))
                throw new ArgumentException("includeWoodenSword must be boolean.");
            lock (server.World.SyncRoot)
            {
                if (expected != dashboardRevision)
                    return Reply(409, Obj("error", "Balance changed. Reload before previewing this draft."));
                var draft = CloneBalance();
                SetProgression(draft, request, values, false);
                var rows = new List<object>();
                var bases = new Dictionary<string, object>();
                foreach (int cls in ids)
                {
                    var anchor = PlayerCombat.ProgressionSample(server.World, draft, cls, 1, false);
                    var baseSample = PlayerCombat.ProgressionSample(server.World, draft, cls, 1);
                    var resolved = new Dictionary<string, object>();
                    var anchorValues = PlayerTuning.Values(anchor, server.World, draft);
                    foreach (string field in GameBalanceRules.PlayerFields)
                    {
                        var rule = draft.CharacterProgression.Resolve(cls, field);
                        resolved[field] = Obj("base", rule.Base ?? Convert.ToInt32(anchorValues[field]), "growthPercent", rule.GrowthPercent ?? 100, "growthFlat", rule.GrowthFlat ?? 0);
                    }

                    bases[cls.ToString()] = Obj("stats", PlayerTuning.Values(baseSample, server.World, draft), "resolved", resolved);
                    PlayerState previous = from > 1 ? PlayerCombat.ProgressionSample(server.World, draft, cls, from - 1) : null;
                    for (int level = from; level <= to; level++)
                    {
                        var sample = PlayerCombat.ProgressionSample(server.World, draft, cls, level);
                        rows.Add(Obj("classId", cls, "scenario", cls == 0 ? "unclassed" : "classed", "level", level, "stats", PlayerTuning.Values(sample, server.World, draft), "deltaHp", previous == null ? 0 : sample.MaxHp - previous.MaxHp, "deltaAttack", previous == null ? 0 : sample.Damage - previous.Damage));
                        previous = sample;
                    }
                }

                if (request.ContainsKey("includeWoodenSword") && (bool)request["includeWoodenSword"])
                {
                    PlayerState previous = from > 1 ? WoodenSwordSample(draft, from - 1) : null;
                    for (int level = from; level <= to; level++)
                    {
                        var sample = WoodenSwordSample(draft, level);
                        rows.Add(Obj("classId", 0, "scenario", "woodenSword", "level", level, "stats", PlayerTuning.Values(sample, server.World, draft), "deltaHp", previous == null ? 0 : sample.MaxHp - previous.MaxHp, "deltaAttack", previous == null ? 0 : sample.Damage - previous.Damage));
                        previous = sample;
                    }
                }

                return Reply(200, Obj("revision", dashboardRevision, "bases", bases, "rows", rows, "draft", true, "saved", false));
            }
        }

        private PlayerState WoodenSwordSample(GameBalanceRules balance, int level)
        {
            var sample = PlayerCombat.ProgressionSample(server.World, balance, 0, level);
            var sword = PlayerItems.CreateTaskWoodenSword();
            var template = server.World.FindItem(sword.Id);
            if (template == null)
                throw new ArgumentException("Wooden sword template is unavailable.");
            sword.Index = template.Type;
            sample.Inventory.Equipment[template.Type] = sword;
            PlayerCombat.SetAbility(sample, server.World, balance);
            return sample;
        }
    }
}
