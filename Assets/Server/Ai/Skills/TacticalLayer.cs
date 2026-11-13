using System;
using System.Collections.Generic;
using NsoServer.Net;
using NsoServer.World;

namespace NsoServer.Ai
{
    using Math = System.Math;
    using Mob = NsoServer.World.Mob;
    using Zone = NsoServer.World.Zone;
    using Waypoint = NsoServer.World.Waypoint;

    /// <summary>
    /// Tactical AI (game AI) of an AI player: plays the current plan step every server tick, like a person at the
    /// controls. It only perceives the character's state (PlayerState: position, HP, bag, quest...) and acts through
    /// BotClient, i.e. the messages the game client sends; the server applies the player's rules to everything
    /// (MapUnlock on map changes, cooldown/MP/range on attacks, shop prices and quest gates, pickup ownership, bag space).
    /// Besides the goals there are habits every player has: drink a potion when HP is low, spend potential and skill
    /// points, wear equipment and read skill books from the bag, pick up its own loot, return to the village after death.
    /// Reports progress, completion and failure back through AgentManager (StepDone / StepFailed / Report).
    /// </summary>
    public static partial class AgentTactics
    {
        private static readonly Random Rng = new Random();
        private enum Status
        {
            Running,
            Done,
            Failed
        }

        // NPC templates (PlayerTasks): Kiriko sells potions, Tabemono food, Tajima gives the wooden sword; school heads
        private const int NpcKiriko = 3, NpcTabemono = 4, NpcKenshinto = 6, NpcTajima = 12, NpcToyotomi = 9, NpcOokamesama = 10, NpcKazeto = 11;
        // shop types (PlayerTasks.ShopPotion / ShopPotionLock / ShopFood / ShopFoodLock)
        private const int ShopPotionXu = 6, ShopPotionYen = 7, ShopFoodXu = 8, ShopFoodYen = 9;
        private const int ItemComNam = 23, ItemWoodenSword = 194;
        private const int TaskLanDauDungKiem = 2, TaskDietSenTruCoc = 3, TaskKhamPha = 6, TaskGiaTangSucManh = 9, TaskNangCapTrangBi = 12;
        private const int ActionGapMs = 900; // a player needs about a second per menu click
        // ================================================================== tick
        /// <summary>One tick (500 ms) of the AI player.</summary>
        public static void Update(NinjaAgent a, GameWorld world, long now)
        {
            var me = a.Me;
            if (me == null || me.Zone == null)
                return;
            if (me.IsDead)
            {
                a.Activity = "dead, waiting to return to the village";
                if (a.DeadSince == 0)
                {
                    a.DeadSince = now;
                    AgentManager.OnDied(a, world);
                }
                else if (now - a.DeadSince > Math.Max(1, AgentConfig.ReviveAfterSeconds) * 1000L)
                    a.Client.ReturnTown(); // Service.returnTownFromDead
                return;
            }

            if (a.DeadSince != 0)
            {
                a.DeadSince = 0;
                AgentManager.Report(a, AgentEvent.Respawned, "came back to life in map " + me.MapId + " " + me.Zone.Tile.Name);
            }

            if (me.IsChangeMap)
                return;
            Habits(a, me, world, now);
            // reflex: low HP overrides the plan (the plan's fleeHp is the strategic constraint)
            // Without potions HP does not come back, so the retreat starts earlier (NoPotionFleeHpPercent) while the
            // shop is affordable: walking to Kiriko through monsters with 20 % HP was the usual death.
            int fleeAt = a.FleeHp;
            if (HpPotions(me, world) == 0 && CanAffordPotions(me, world))
                fleeAt = Math.Max(fleeAt, AgentConfig.NoPotionFleeHpPercent);
            if (!a.Recovering && HpPercent(me) < fleeAt && !DrinkHp(a, me, world, now) && now >= a.NoPotionsUntil)
            {
                a.Recovering = true;
                AgentManager.Report(a, AgentEvent.LowHp, "HP fell to " + HpPercent(me) + "% with no potion left");
            }

            if (a.Recovering)
            {
                if (Recover(a, me, world, now) != Status.Running)
                    a.Recovering = false;
                return;
            }

            var step = a.Step;
            if (TryLootHabit(a, me, world, now))
                return;
            if (step == null)
            {
                Wander(a, me, world, 120);
                a.Activity = a.Pending ? "waiting for a decision" : "idle";
                return;
            }

            if (now - Math.Max(a.StepStartedAt, a.LastProgressAt) > Math.Max(10, AgentConfig.StepTimeoutSeconds) * 1000L)
            {
                AgentManager.StepFailed(a, world, now, "no progress for " + AgentConfig.StepTimeoutSeconds + " s (" + step + ")");
                return;
            }

            string why = null;
            Status st;
            switch (step.Goal)
            {
                case GoalType.Travel:
                    st = Travel(a, me, world, step.MapId, out why);
                    a.Activity = "travelling to map " + step.MapId;
                    break;
                case GoalType.Hunt:
                    st = Hunt(a, me, world, step.MapId, step.MobId, step.Count, now, out why);
                    break;
                case GoalType.Collect:
                    st = Collect(a, me, world, step, now);
                    break;
                case GoalType.Rest:
                    st = Recover(a, me, world, now);
                    break;
                case GoalType.Shop:
                    st = step.MobId >= 0 ? TryItem(a, me, world, now, step.MobId, out why) : Shop(a, me, world, now, out why);
                    break;
                case GoalType.Quest:
                    st = QuestPlay.Run(a, me, world, now, out why);
                    break;
                case GoalType.Explore:
                    st = Explore(a, me, world, step, now, out why);
                    break;
                case GoalType.Move:
                    st = MoveTo(a, me, world, step, out why);
                    break;
                default:
                    st = Idle(a, me, world, step, now);
                    break;
            }

            if (st == Status.Done)
                AgentManager.StepDone(a, world, now);
            else if (st == Status.Failed)
                AgentManager.StepFailed(a, world, now, why ?? "failed");
        }

        public static int HpPercent(PlayerState me)
        {
            return me.MaxHp <= 0 ? 0 : (int)((long)me.Hp * 100 / me.MaxHp);
        }

        public static int MpPercent(PlayerState me)
        {
            return me.MaxMp <= 0 ? 0 : (int)((long)me.Mp * 100 / me.MaxMp);
        }

        // names of the NPCs the quests use (PlayerTasks constants; npc_template is not kept by GameWorld)
        private static readonly Dictionary<int, string> NpcNames = new Dictionary<int, string>
        {
            {
                0,
                "Kanata"
            },
            {
                1,
                "Furoya"
            },
            {
                2,
                "Ameji"
            },
            {
                3,
                "Kiriko"
            },
            {
                4,
                "Tabemono"
            },
            {
                5,
                "Kamakura"
            },
            {
                6,
                "Kenshinto"
            },
            {
                7,
                "Umayaki"
            },
            {
                8,
                "Umayaki"
            },
            {
                9,
                "Toyotomi"
            },
            {
                10,
                "Ookamesama"
            },
            {
                11,
                "Kazeto"
            },
            {
                12,
                "Tajima"
            },
            {
                23,
                "Matsurugi"
            },
            {
                24,
                "Okanechan"
            },
            {
                26,
                "Goosho"
            },
        };
    }
}
