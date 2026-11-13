using System;
using System.Collections.Generic;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    // the Unity client also defines global types with these names, so pick the server ones explicitly
    using Zone = NsoServer.World.Zone;
    using Mob = NsoServer.World.Mob;
    using Math = System.Math; // the client has its own global Math class

    // Port of the player-attacks-monster loop of the Java server, ordinary maps only:
    //   Char.selectSkill, Char.attackMonster(Message) + attackMonster(ArrayList<Mob>), Char.addExp(Mob, int) + addExp(long),
    //   AbilityFromEquip.setAbility (equipment part), Mob.die / Mob.recovery, Zone.updateRecovery,
    //   MapService.attackMonster / setSkillPaint_1 / recoveryMonster / loadLevel / refreshHP, Service.addExp / levelUp / updateMp.
    // Not ported yet (no data or feature behind them offline): skill side effects (fire / ice / wind / stun), clones,
    // pets, support skills, mount / mask / bijuu options, item drops, quest and daily-task counters, dungeons and wars.
    public static class PlayerCombat
    {
        // sub commands / commands used here (values identical to com.nsoz.constants.CMD)
        private const sbyte NPC_HP = -1, NPC_DIE = -4, NPC_LIVE = -5, NPC_MISS = 51, PLAYER_ATTACK_NPC = 60;
        private const sbyte PLAYER_UP_EXP = 5, PLAYER_UP_EXPDOWN = 71, SERVER_MESSAGE = -24;
        private const sbyte ME_LOAD_LEVEL = -124, ME_LOAD_MP = -121, PLAYER_LOAD_LEVEL = -128, REFRESH_HP = -59;
        private const int MobBuNhin = 0, MobHopBiAn = 225;
        private const int RateExp = 1; // config.properties game.server.exp
        private const int FreshMp = 100; // MP of a new character as captured from the Java server
        private static readonly Random Rng = new Random();
        // NinjaUtils.nextInt(min, max): inclusive, returns max when min >= max
        private static int NextInt(int min, int max)
        {
            if (min >= max)
                return max;
            long span = (long)max + 1 - min;
            if (span > int.MaxValue)
                return (int)(min + (long)(Rng.NextDouble() * span));
            return Rng.Next(max + 1 - min) + min;
        }

        // NinjaUtils.nextInt(max): 0 .. max-1
        private static int NextInt(int max)
        {
            return max <= 0 ? 0 : Rng.Next(max);
        }

        private static long Now()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // ---------------------------------------------------------------- stats
        // Called when the character enters the world: level from experience, potentials, inventory, basic skill, stats.
        public static void Init(PlayerState me, GameWorld world)
        {
            me.Level = world.GetLevel(me.Record.Exp);
            me.Inventory = PlayerInventory.Load(me.Record.Data, world);
            me.Progress = PlayerProgress.Load(me.Record.Data);
            PlayerTasks.SkipRemovedTask(me); // offline: NV Bạn hữu tâm giao removed
            me.PotentialPoint = me.Progress.PotentialPoint;
            me.SkillPoint = me.Progress.SkillPoint;
            if (me.Record.ClassId == 0)
                SetClassZeroPotential(me);
            else if (me.Progress.Potential == null)
                Array.Copy(CharacterProgressionRules.OriginalPotential(me.Record.ClassId), me.Potential, 4);
            else
                for (int i = 0; i < 4; i++)
                    me.Potential[i] = me.Progress.Potential[i];
            SyncBasicSkill(me); // saves made before the basic attack grew
            // the first attack skill the character knows (a new character: the basic attack, template 0)
            PlayerSkills.Select(me, world, -1);
            SetAbility(me, world);
            me.Hp = me.MaxHp;
            me.Mp = me.MaxMp;
        }

        // Char.addExp: a character without a class gets +2/+1/+1/+1 potential per level. Starting values are 15/5/5/5,
        // so the potentials follow from the level. Classed characters retain their saved manual allocations.
        // Offline deviation (BasicSkillGrowth): without a school the basic attack gets one point per level, spent
        // automatically. Returns true when the point changed.
        private static bool SyncBasicSkill(PlayerState me)
        {
            if (me.Record.ClassId != 0)
                return false;
            int want = BasicSkillGrowth.PointForLevel(me.Level);
            foreach (var s in me.Progress.Skills)
            {
                if (s[0] != BasicSkillGrowth.TemplateId || s[1] == want)
                    continue;
                s[1] = want;
                return true;
            }

            return false;
        }

        private static void SetClassZeroPotential(PlayerState me)
        {
            int up = Math.Max(0, me.Level - 1);
            var basis = CharacterProgressionRules.OriginalPotential(0);
            var growth = CharacterProgressionRules.OriginalGrowth(0);
            for (int i = 0; i < 4; i++)
                me.Potential[i] = basis[i] + growth[i] * up;
        }

        // Char.getSideClass: classes 2, 4, 6 hit with inner strength (potential 3), the others with outer (potential 0)
        private static int SideClass(int classId)
        {
            return classId == 2 || classId == 4 || classId == 6 ? 1 : 0;
        }

        // Char.getSys: element of the class (1 fire, 2 ice, 3 wind, 0 none)
        private static int ClassSys(int classId)
        {
            if (classId == 1 || classId == 2)
                return 1;
            if (classId == 3 || classId == 4)
                return 2;
            if (classId == 5 || classId == 6)
                return 3;
            return 0;
        }

        // AbilityFromEquip.setAbility, with equipment and fashion options and the selected skill's options.
        public static void SetAbility(PlayerState me, GameWorld world, GameBalanceRules balance = null, bool applyProgression = true)
        {
            balance = balance ?? world.BalanceRules;
            int previousHp = me.Hp, previousMp = me.Mp;
            int[] rawOptions = me.Options;
            Array.Clear(rawOptions, 0, rawOptions.Length);
            if (me.Inventory != null)
            {
                AddOptions(rawOptions, me.Inventory.Equipment, world, balance);
                AddOptions(rawOptions, me.Inventory.Fashion, world, balance);
            }

            // Wide intermediates prevent overflow under global equipment/stat balancing.
            long[] o = Array.ConvertAll(rawOptions, value => (long)value);
            var support = new int[o.Length]; // Char.optionsSupportSkill: passive skills
            PlayerSkills.AddSupportOptions(me, world, support);
            var skillOptions = new int[o.Length];
            if (me.SelectedSkill != null)
                for (int i = 0; i < me.SelectedSkill.OptionIds.Length; i++)
                {
                    int id = me.SelectedSkill.OptionIds[i];
                    if (id >= 0 && id < skillOptions.Length)
                        skillOptions[id] += me.SelectedSkill.OptionParams[i];
                }

            long[] pot = Array.ConvertAll(PlayerTuning.Potentials(me, balance), value => (long)value); // Global balance never rewrites saved potential points.
            me.Exactly = GameBalanceRules.Stat(pot[1] + (pot[1] * o[58] / 100) + o[57] + o[12]);
            me.Miss = GameBalanceRules.Stat((pot[1] + o[57] + (pot[1] * o[58] / 100)) * 150 / 100);
            long potentialDame;
            if (SideClass(me.Record.ClassId) == 0)
            {
                potentialDame = (pot[0] + o[57]) + (pot[0] * o[58] / 100);
                long basicAttack = potentialDame + o[38] + (potentialDame * skillOptions[11] / 100);
                basicAttack += potentialDame * PlayerBuff.IncrDame2(me) / 100; // incrDame2 (effect type 13)
                me.Damage = GameBalanceRules.Stat((basicAttack + o[0] + o[21] + o[23] + o[25]) + (potentialDame * o[8] / 100) + potentialDame);
            }
            else
            {
                potentialDame = pot[3] + o[57] + (pot[3] * o[58] / 100);
                long basicAttack = potentialDame + o[38] + (potentialDame * skillOptions[11] / 100);
                basicAttack += potentialDame * PlayerBuff.IncrDame2(me) / 100; // incrDame2 (effect type 13)
                me.Damage = GameBalanceRules.Stat((basicAttack + o[1] + o[22] + o[24] + o[26]) + (potentialDame * o[9] / 100) + potentialDame);
            }

            me.Damage = GameBalanceRules.Stat((long)me.Damage + (o[73] + o[87] + o[76]));
            me.Damage = GameBalanceRules.Stat((long)me.Damage + ((potentialDame * 4) * o[94] / 100));
            me.Damage = GameBalanceRules.Stat((long)me.Damage + (support[8] + support[6] + support[9] + support[7] + support[1]));
            for (int i = 2; i <= 10; i++)
                me.Damage = GameBalanceRules.Stat((long)me.Damage + (skillOptions[i]));
            switch (ClassSys(me.Record.ClassId))
            {
                case 1:
                    me.Damage = GameBalanceRules.Stat((long)me.Damage + (o[88]));
                    break;
                case 2:
                    me.Damage = GameBalanceRules.Stat((long)me.Damage + (o[89]));
                    break;
                case 3:
                    me.Damage = GameBalanceRules.Stat((long)me.Damage + (o[90]));
                    break;
            }

            me.Damage2 = GameBalanceRules.Stat(me.Damage - me.Damage / 10);
            long basicHp = (pot[2] + o[57] + (pot[2] * o[58] / 100)) * 10;
            me.MaxHp = GameBalanceRules.Stat(basicHp + basicHp * (o[31] + o[61] + support[17]) / 100);
            me.MaxHp = GameBalanceRules.Stat((long)me.MaxHp + (o[6] + o[32] + o[77] + o[82] + o[125]));
            me.MaxHp = GameBalanceRules.Stat((long)me.MaxHp + (me.MaxHp * o[128] / 100));
            if (me.MaxHp <= 0)
                me.MaxHp = GameBalanceRules.Stat(50);
            if (me.Hp > me.MaxHp)
                me.Hp = me.MaxHp;
            long basicMp = (pot[3] + o[57] + (pot[3] * o[58] / 100)) * 10;
            long maxMp = basicMp + (basicMp + (o[28] + o[60] + support[18])) / 100;
            maxMp += o[7] + o[19] + o[29] + o[83] + o[117];
            // Offline: never below the 100 MP the Java server gives a new character (captured in enter_map22).
            me.MaxMp = GameBalanceRules.Stat(Math.Max(FreshMp, maxMp));
            if (me.Mp > me.MaxMp)
                me.Mp = me.MaxMp;
            me.DameDown = GameBalanceRules.Stat(o[47] + o[74] + o[80] + o[124]);
            me.Miss = GameBalanceRules.Stat((long)me.Miss + (o[5] + o[17] + o[62] + o[68] + o[78] + o[84] + o[115] + support[13]));
            me.Exactly = GameBalanceRules.Stat((long)me.Exactly + (o[10] + o[18] + o[75] + o[86] + o[116] + support[12]));
            me.ResFire = GameBalanceRules.Stat(o[2] + o[11] + o[33] + o[70] + o[96] + support[19] + support[20] + o[36] + o[118]);
            me.ResIce = GameBalanceRules.Stat(o[3] + o[12] + o[34] + o[71] + o[95] + support[19] + support[21] + o[36] + o[118]);
            me.ResWind = GameBalanceRules.Stat(o[4] + o[13] + o[35] + o[72] + o[97] + support[19] + support[22] + o[36] + o[118]);
            me.Fatal = GameBalanceRules.Stat(o[14] + o[37] + o[69] + o[92] + o[114] + support[14]);
            me.SupportOptions = support;
            me.FatalDame = GameBalanceRules.Stat(o[105]);
            me.PercentFatalDame = GameBalanceRules.Stat(o[39] + o[67]);
            PlayerTuning.ApplyAbility(me, world, balance, applyProgression); // Shared gameplay and draft-preview calculation.
            // Intermediate base-stat clamps must not drain resources under a larger dashboard maximum.
            me.Hp = Math.Min(previousHp, me.MaxHp);
            me.Mp = Math.Min(previousMp, me.MaxMp);
        }

        // Preview and native-curve anchors reuse the gameplay calculator, with no session, packets or save.
        public static PlayerState ProgressionSample(GameWorld world, GameBalanceRules balance, int cls, int level, bool applyProgression = true)
        {
            var p = new PlayerState
            {
                Record = new PlayerRecord
                {
                    ClassId = (sbyte)cls
                },
                Level = level,
                Inventory = PlayerInventory.Load("{}", world),
                Progress = PlayerProgress.Load("{}")
            };
            var basis = CharacterProgressionRules.OriginalPotential(cls);
            var growth = CharacterProgressionRules.OriginalGrowth(cls);
            for (int i = 0; i < 4; i++)
                p.Potential[i] = basis[i] + growth[i] * (level - 1);
            p.SelectedSkill = cls == 0 ? world.FindSkill(0, BasicSkillGrowth.PointForLevel(level)) : null;
            SetAbility(p, world, balance, applyProgression);
            return p;
        }

        // option types 3..7 only count once the item is upgraded to +4 / +8 / +12 / +14 / +16
        private static void AddOptions(int[] o, PlayerItem[] items, GameWorld world, GameBalanceRules balance)
        {
            foreach (var item in items)
            {
                if (item == null)
                    continue;
                if (item.Expire > 0 && item.Expire < Now())
                    continue;
                foreach (var opt in balance.EffectiveOptions(item))
                {
                    if (opt.Id < 0 || opt.Id >= o.Length)
                        continue;
                    int type = world.ItemOptionType(opt.Id);
                    if (type >= 3 && type <= 7)
                    {
                        bool active = (type == 3 && item.Upgrade >= 4) || (type == 4 && item.Upgrade >= 8) || (type == 5 && item.Upgrade >= 12) || (type == 6 && item.Upgrade >= 14) || (type == 7 && item.Upgrade >= 16);
                        if (!active)
                            continue;
                    }

                    o[opt.Id] += opt.Param;
                }
            }
        }

        // ---------------------------------------------------------------- skill select (cmd 41)
        // Char.selectSkill(Message): see PlayerSkills.SelectSkill
        public static void SelectSkill(Message m, PlayerState me, GameWorld world)
        {
            PlayerSkills.SelectSkill(m, me, world);
        }

        // ---------------------------------------------------------------- attack (cmd 60)
        // Char.attackMonster(Message): the client sends the ids of the monsters it hits (one unsigned byte each).
        public static void AttackMonster(Message m, PlayerState me, GameWorld world)
        {
            if (me.IsDead)
                return;
            var skill = me.SelectedSkill;
            if (skill == null)
                return;
            long now = Now();
            if (now - me.LastTimeUseSkill < skill.CoolDown)
                return; // Skill.isCooldown
            me.LastTimeUseSkill = now;
            // a weapon is needed (the wooden sword comes from NV kiến thức or Tajima's "Nhận kiếm gỗ")
            if (me.Inventory == null || me.Inventory.Equipment[1] == null)
            {
                ServerMessage(me, "Vũ khí không thích hợp");
                return;
            }

            // Char.isMeCanAttackNpc: buff / NPC skills do not hit monsters
            var tpl = world.FindSkillTemplate(skill.TemplateId);
            if (tpl != null && (tpl.Type == PlayerSkills.TypeBuff || tpl.Type == PlayerSkills.TypeNpc || tpl.Type == PlayerSkills.TypeLive))
                return;
            var zone = me.Zone;
            var hit = new List<Mob>(); // monsters actually in range (mobs)
            var sent = new List<Mob>(); // live monsters the client named (m)
            while (m.Reader.Remaining > 0)
            {
                int id = m.Reader.ReadByte();
                if (id == 127)
                    continue; // own pet: no pets offline
                Mob mob = FindLiveMob(zone, id);
                if (mob != null && mob.Hp > 0)
                    sent.Add(mob);
            }

            Mob focus = null;
            if (sent.Count > 0)
            {
                Mob mob = sent[0];
                int left = me.X - skill.Dx, right = me.X + skill.Dx, top = me.Y - skill.Dy - 75, bottom = me.Y + skill.Dy;
                int add = mob.Template.Type == 4 ? 50 : 0; // flying monsters can be hit from further below
                int range = mob.Template.RangeMove;
                if (mob.X >= left - range && mob.X <= right + range && mob.Y >= top - range - add && mob.Y <= bottom + range)
                {
                    focus = mob;
                    hit.Add(mob);
                }
            }

            if (focus != null)
            {
                // area skills also hit other monsters within 100 px of the focus, up to max_fight
                int count = Math.Min(sent.Count, skill.MaxFight);
                for (int i = 0; i < count; i++)
                {
                    Mob other = sent[i];
                    if (other.Status != 1 && other.Status != 0 && other != focus && focus.X - 100 <= other.X && other.X <= focus.X + 100 && focus.Y - 100 <= other.Y && other.Y <= focus.Y + 100 && !hit.Contains(other))
                        hit.Add(other);
                }
            }
            else
            {
                // out of range: every named monster shows "miss"
                foreach (var mob in sent)
                    SendHitResult(zone, -1, false, mob);
                SkillPaint(zone, me, sent, skill.TemplateId);
            }

            if (hit.Count == 0)
                return;
            if (me.Mp < skill.ManaUse)
            {
                ServerMessage(me, "Không đủ MP để sử dụng");
                UpdateMp(me);
                return;
            }

            me.Mp -= skill.ManaUse;
            UpdateMp(me);
            Hit(me, world, hit, skill);
        }

        private static Mob FindLiveMob(Zone zone, int id)
        {
            foreach (var mob in zone.Mobs)
                if (mob.Id == id && !mob.IsDead)
                    return mob;
            return null;
        }

        // Char.attackMonster(ArrayList<Mob>): damage, critical hits, misses, death and experience.
        private static void Hit(PlayerState me, GameWorld world, List<Mob> mobs, SkillData skill)
        {
            var zone = me.Zone;
            int tplId = skill.TemplateId;
            int param0 = skill.OptionParams.Length > 0 ? skill.OptionParams[0] : 0;
            // skill side effects on monsters (Char.attackMonster(ArrayList))
            if (tplId == 4)
            {
                // Kakyuu: roasts an ordinary monster into sushi
                Mob first = mobs[0];
                if (!first.IsBoss && first.LevelBoss == 0)
                {
                    DieBy(me, world, zone, first);
                    PlayerItems.DropSushi(me, world, zone, first, param0);
                    SendHitResult(zone, 0, false, first);
                }
                else
                    ServerMessage(me, "Đối phương quá mạnh bạn không thể thi triển thuật này!");
                return;
            }

            if (tplId == 25 || tplId == 34)
            {
                if (NextInt(100) <= param0)
                    PlayerEffects.AddMobEffect(zone, mobs[0], 2, 3000);
            }
            else if (tplId == 24)
            {
                PlayerEffects.AddMobEffect(zone, mobs[0], 14, param0 * 1000L);
                return;
            }
            else if (tplId == 40)
            {
                PlayerEffects.AddMobEffect(zone, mobs[0], 0, param0 * 1000L);
                return;
            }
            else if (tplId == 43)
            {
                if (NextInt(100) <= param0)
                    PlayerEffects.AddMobEffect(zone, mobs[0], 3, 2000);
            }
            else if (tplId == 7 || tplId == 16)
            {
                if (NextInt(100) <= param0)
                    PlayerEffects.AddMobEffect(zone, mobs[0], 1, 4000);
            }
            else if (Array.IndexOf(FireSkills, tplId) >= 0)
            {
                foreach (var mb in mobs)
                    if (NextInt(100) <= param0)
                        PlayerEffects.AddMobEffect(zone, mb, 1, 2000);
            }
            else if (Array.IndexOf(IceSkills, tplId) >= 0)
            {
                foreach (var mb in mobs)
                    if (NextInt(100) <= param0)
                        PlayerEffects.AddMobEffect(zone, mb, 2, 2000);
            }
            else if (Array.IndexOf(WindSkills, tplId) >= 0)
            {
                foreach (var mb in mobs)
                    if (NextInt(100) <= param0)
                        PlayerEffects.AddMobEffect(zone, mb, 3, 1000);
            }
            else if (me.SupportOptions != null && me.SupportOptions[69] > 0)
            {
                foreach (var mb in mobs)
                    if (NextInt(100) <= me.SupportOptions[69])
                        PlayerEffects.AddMobEffect(zone, mb, 2, 2000);
            }

            bool isFatal = me.Fatal > NextInt(1000); // 1000 = 100 % critical chance
            int invisibleAdd = 0;
            var invisible = PlayerBuff.Find(me, PlayerBuff.TypeInvisible);
            if (invisible != null)
            {
                isFatal = true;
                invisibleAdd = me.SupportOptions != null ? me.SupportOptions[61] : 0;
                PlayerBuff.Remove(me, invisible);
            }

            var burnHit = PlayerBuff.Find(me, PlayerBuff.TypeBurnHit); // isEffBong: hit monsters start burning
            SkillPaint(zone, me, mobs, skill.TemplateId);
            foreach (var mob in mobs)
            {
                if (mob.IsDead)
                    continue;
                int miss = NextInt(mob.Level + 10);
                int exactly = NextInt(GameBalanceRules.Stat((long)me.Exactly + 100));
                bool isMiss = miss > exactly;
                if (tplId == 7 || tplId == 25 || tplId == 34 || tplId == 43)
                    isMiss = false;
                // bosses ignore characters more than 20 levels away (ordinary maps)
                if (mob.IsBoss && Math.Abs(me.Level - mob.Level) > 20)
                    isMiss = true;
                if (isMiss)
                {
                    SendHitResult(zone, -1, false, mob);
                    continue;
                }

                if (burnHit != null)
                {
                    // Effect eff2 = new Effect(9, eff.getTimeRemaining(), eff.param); eff2.param2 = id; mob.effects.put(5, eff2)
                    mob.BurnEnd = burnHit.EndAt;
                    mob.BurnParam = burnHit.Param;
                    mob.BurnOwner = me.Record.Id;
                }

                int damageBasic = NextInt(me.Damage2, me.Damage);
                long hit = damageBasic;
                if (isFatal)
                {
                    hit += damageBasic;
                    hit += (long)damageBasic * me.PercentFatalDame / 100;
                    hit += me.FatalDame;
                }

                hit += me.Options[102];
                hit += me.Options[113];
                hit += invisibleAdd;
                hit = Math.Min(int.MaxValue, hit); // Bound the intermediate before elemental percent bonuses.
                switch (mob.Sys)
                {
                    case 1:
                        hit += me.Options[51];
                        hit += hit * me.Options[54] / 100;
                        break;
                    case 2:
                        hit += me.Options[52];
                        hit += hit * me.Options[55] / 100;
                        break;
                    case 3:
                        hit += me.Options[53];
                        hit += hit * me.Options[56] / 100;
                        break;
                }

                if (hit <= 0)
                    continue;
                int dameHit = GameBalanceRules.Stat(hit);
                int preHp = mob.Hp;
                // the training dummy (Bù nhìn) always loses a fifth of its HP
                if (mob.Template.Id == MobBuNhin)
                    mob.Hp -= mob.MaxHp / 5;
                else
                    mob.Hp -= dameHit;
                if (mob.Hp < 0)
                    mob.Hp = 0;
                AddExp(me, world, mob, Math.Abs(mob.Hp - preHp));
                if (mob.Hp <= 0)
                    DieBy(me, world, zone, mob);
                else if (mob.Id != 0 && !mob.Chars.Contains(me.Record.Id))
                    mob.Chars.Add(me.Record.Id); // it fights back
                SendHitResult(zone, dameHit, isFatal, mob);
            }
        }

        /// <summary>Mob.callFireEffect: a burning monster loses param HP each update, credited to the player who set it.</summary>
        public static void FireTick(Zone zone, Mob mob, long now)
        {
            if (mob.BurnEnd <= 0)
                return;
            if (mob.IsDead || now >= mob.BurnEnd)
            {
                mob.BurnEnd = 0;
                return;
            }

            PlayerState p = null;
            foreach (PlayerState x in zone.Players)
                if (x.Record.Id == mob.BurnOwner)
                {
                    p = x;
                    break;
                }

            if (p == null)
                return;
            var world = p.Session.Server.World;
            int damage = mob.BurnParam;
            int preHp = mob.Hp;
            if (mob.Template.Id == MobBuNhin)
                mob.Hp -= mob.MaxHp / 5;
            else
                mob.Hp -= damage;
            if (mob.Hp < 0)
                mob.Hp = 0;
            AddExp(p, world, mob, Math.Abs(mob.Hp - preHp));
            if (mob.Hp <= 0)
            {
                mob.BurnEnd = 0;
                DieBy(p, world, zone, mob);
            }

            SendHitResult(zone, damage, false, mob);
        }

        // skill templates that may burn / freeze / stun every monster they hit
        private static readonly int[] FireSkills =
        {
            9,
            61,
            73,
            79,
            18,
            62,
            78,
            83,
            85,
            86
        };
        private static readonly int[] IceSkills =
        {
            27,
            63,
            75,
            81,
            36,
            64,
            76,
            82,
            87,
            88
        };
        private static readonly int[] WindSkills =
        {
            45,
            65,
            74,
            80,
            54,
            66,
            77,
            84,
            89,
            90
        };
        // Mob.die + Mob.dead: the monster dies, drops its items and counts for the quest
        private static void DieBy(PlayerState killer, GameWorld world, Zone zone, Mob mob)
        {
            Die(zone, mob);
            PlayerItems.OnMobDead(killer, world, zone, mob);
            TaskOrders.OnKill(killer, mob); // Mob.dead: killer.taskOrders
            PlayerTasks.OnKill(killer, world, mob);
        }

        // Mob.die + the respawn part of Mob.dead
        private static void Die(Zone zone, Mob mob)
        {
            if (mob.LevelBoss == 2)
                zone.NumberChief = Math.Max(0, zone.NumberChief - 1);
            else if (mob.LevelBoss == 1)
                zone.NumberElitez = Math.Max(0, zone.NumberElitez - 1);
            mob.Hp = 0;
            mob.Status = 0;
            mob.IsDead = true;
            mob.RecoveryTimeCount = mob.IsBeast ? 300 : 20; // zone ticks of half a second: 10 s, beasts 150 s
            mob.Chars.Clear();
            // ordinary maps: monsters come back, bosses do not (except the mystery box)
            if ((!mob.IsBoss || mob.Template.Id == MobHopBiAn) && !zone.NoRespawn)
                mob.IsWaitingRespawn = true; // instances: isCantRespawn
        }

        // ---------------------------------------------------------------- experience
        // Char.addExp(Mob, int): experience from the HP taken off a monster within 10 levels
        private static void AddExp(PlayerState me, GameWorld world, Mob mob, int dame)
        {
            int dLevel = Math.Abs(mob.Level - me.Level);
            if (mob.Template.Id == MobBuNhin || dLevel > 10)
                return;
            int a = me.Level / 20;
            if (a == 0)
                a = 1;
            int b = dame * a;
            int c = (mob.Level - me.Level) * a;
            long exp = (b / 2) + ((long)b * c / 100);
            if (mob.LevelBoss == 1)
                exp *= 2;
            else if (mob.LevelBoss == 2)
                exp *= 5;
            exp += exp * me.Options[100] / 100;
            var incr = PlayerBuff.Find(me, 18); // incrExp: "nhân 2 / 3 kinh nghiệm" (Nấm linh chi...)
            if (incr != null && incr.Param > 0)
                exp *= incr.Param;
            if (exp > 0)
                AddExp(me, world, exp);
        }

        // Char.addExp(long): pays off lost experience first, then levels up
        public static void AddExp(PlayerState me, GameWorld world, long exp, bool applyBalance = true)
        {
            exp = (long)Math.Min(world.ExpMax, (decimal)exp * RateExp * (applyBalance ? world.BalanceRules.ExperiencePercent : 100) / 100m);
            if (exp <= 0)
                return;
            if (me.ExpDown > 0)
            {
                long paid = Math.Min(me.ExpDown, exp);
                me.ExpDown -= paid;
                var down = new Message(PLAYER_UP_EXPDOWN);
                down.Writer.WriteLong(paid);
                me.Session.Send(down);
                exp -= paid;
                if (exp <= 0)
                    return;
            }

            me.Record.Exp += exp;
            if (me.Record.Exp > world.ExpMax)
            {
                me.Record.Exp = world.ExpMax;
                return;
            }

            var up = new Message(PLAYER_UP_EXP);
            up.Writer.WriteLong(exp);
            me.Session.Send(up);
            int preLevel = me.Level;
            me.Level = world.GetLevel(me.Record.Exp);
            if (me.Level <= preLevel)
                return;
            int num = me.Level - preLevel;
            if (me.Record.ClassId == 0)
            {
                SetClassZeroPotential(me);
                if (SyncBasicSkill(me))
                {
                    PlayerSkills.Select(me, world, BasicSkillGrowth.TemplateId); // faster row for the attack
                    PlayerService.LoadSkill(me, world);
                    PlayerService.SkillBars(me); // the bars must point at the new basic-attack row
                }
            }
            else
            {
                for (int i = preLevel; i < me.Level; i++)
                    me.PotentialPoint += i >= 10 && i <= 69 ? 10 : i <= 79 ? 20 : i <= 89 ? 30 : i <= 99 ? 40 : i <= 130 ? 50 : i <= 145 ? 60 : 70;
                me.SkillPoint += num;
            }

            SetAbility(me, world);
            SendLevelUp(me);
            SendLoadLevel(me);
            RefreshHp(me);
            PlayerTasks.UpdateTaskLevelUp(me, world); // step "reach level N"
            PlayerItems.Save(me);
        }

        // ---------------------------------------------------------------- respawn (zone tick)
        // Zone.updateRecovery: every half-second tick the dead monsters count down and come back
        public static void UpdateRecovery(Zone zone, GameWorld world)
        {
            foreach (var mob in zone.Mobs)
            {
                if (!mob.IsWaitingRespawn)
                    continue;
                mob.RecoveryTimeCount--;
                if (mob.RecoveryTimeCount > 0)
                    continue;
                mob.IsWaitingRespawn = false;
                world.RecoverMob(zone, mob);
                SendRecovery(zone, mob);
            }
        }

        // ---------------------------------------------------------------- messages
        private static void SendZone(Zone zone, Message m)
        {
            foreach (PlayerState p in zone.Players)
                p.Session.Send(m);
        }

        // MapService.setSkillPaint_1: everybody sees the attack animation
        private static void SkillPaint(Zone zone, PlayerState me, List<Mob> mobs, int skillTemplateId)
        {
            var m = new Message(PLAYER_ATTACK_NPC);
            m.Writer.WriteInt(me.Record.Id);
            m.Writer.WriteByte(skillTemplateId);
            foreach (var mob in mobs)
                m.Writer.WriteByte(mob.Id);
            SendZone(zone, m);
        }

        // MapService.attackMonster: death (with no dropped item), miss, or the HP left
        private static void SendHitResult(Zone zone, int damage, bool fatal, Mob mob)
        {
            Message m;
            if (mob.IsDead)
            {
                m = new Message(NPC_DIE);
                m.Writer.WriteByte(mob.Id);
                m.Writer.WriteInt(damage);
                m.Writer.WriteBoolean(fatal);
            }
            else if (damage == -1)
            {
                m = new Message(NPC_MISS);
                m.Writer.WriteByte(mob.Id);
                m.Writer.WriteInt(mob.Hp);
            }
            else
            {
                m = new Message(NPC_HP);
                m.Writer.WriteByte(mob.Id);
                m.Writer.WriteInt(mob.Hp);
                m.Writer.WriteInt(damage);
                m.Writer.WriteBoolean(fatal);
                m.Writer.WriteByte(mob.LevelBoss);
                m.Writer.WriteInt(mob.MaxHp);
            }

            SendZone(zone, m);
        }

        // MapService.recoveryMonster: a chief/elite is announced as an ordinary monster
        private static void SendRecovery(Zone zone, Mob mob)
        {
            int levelBoss = mob.LevelBoss, maxHp = mob.MaxHp;
            if ((levelBoss == 1 || levelBoss == 2) && !mob.HasTunedHp)
            {
                levelBoss = 0;
                maxHp = mob.Template.Hp;
            }

            var m = new Message(NPC_LIVE);
            m.Writer.WriteByte(mob.Id);
            m.Writer.WriteByte(mob.Sys);
            m.Writer.WriteByte(levelBoss);
            m.Writer.WriteInt(maxHp);
            SendZone(zone, m);
        }

        private static Message Sub(sbyte sub)
        {
            var m = new Message(Cmd.SUB_COMMAND);
            m.Writer.WriteSByte(sub);
            return m;
        }

        // Service.levelUp
        private static void SendLevelUp(PlayerState me)
        {
            var m = Sub(ME_LOAD_LEVEL);
            m.Writer.WriteByte(me.Speed);
            m.Writer.WriteInt(me.MaxHp);
            m.Writer.WriteInt(me.MaxMp);
            m.Writer.WriteLong(me.Record.Exp);
            m.Writer.WriteShort(me.SkillPoint);
            m.Writer.WriteShort(me.PotentialPoint);
            m.Writer.WriteShort(me.Potential[0]);
            m.Writer.WriteShort(me.Potential[1]);
            m.Writer.WriteInt(me.Potential[2]);
            m.Writer.WriteInt(me.Potential[3]);
            me.Session.Send(m);
        }

        // MapService.loadLevel: the zone sees the new level
        private static void SendLoadLevel(PlayerState me)
        {
            var m = Sub(PLAYER_LOAD_LEVEL);
            m.Writer.WriteInt(me.Record.Id);
            m.Writer.WriteInt(me.Hp);
            m.Writer.WriteInt(me.MaxHp);
            m.Writer.WriteByte(me.Level);
            SendZone(me.Zone, m);
        }

        // MapService.refreshHP (sent by Char.setAbility)
        private static void RefreshHp(PlayerState me)
        {
            var m = Sub(REFRESH_HP);
            m.Writer.WriteInt(me.Record.Id);
            m.Writer.WriteInt(me.Hp);
            m.Writer.WriteInt(me.MaxHp);
            SendZone(me.Zone, m);
        }

        // Service.updateMp
        private static void UpdateMp(PlayerState me)
        {
            var m = Sub(ME_LOAD_MP);
            m.Writer.WriteInt(me.Mp);
            me.Session.Send(m);
        }

        // AbsService.serverMessage
        private static void ServerMessage(PlayerState me, string text)
        {
            var m = new Message(SERVER_MESSAGE);
            m.Writer.WriteUTF(text);
            me.Session.Send(m);
        }
    }
}
