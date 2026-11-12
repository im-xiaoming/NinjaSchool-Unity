using System;
using NsoServer.Store;
using NsoServer.World;

namespace NsoServer.Net
{
    using Math = System.Math;   // the client has its own global Math class

    // Port of the skill side of com.nsoz.model.Char: learnSkill (martial-art books), upSkill, upPotential, selectSkill.
    // Learned skills are kept in PlayerProgress.Skills as [template id, point] and turned into `skill` rows on demand.
    public static class PlayerSkills
    {
        // Skill template types (Skill.java)
        public const int TypeAuto = 0, TypeAttack = 1, TypeBuff = 2, TypeNpc = 3, TypeLive = 4;

        // Char.learnSkill: book item id -> skill template id
        private static int BookToTemplate(int itemId)
        {
            if (itemId >= 40 && itemId <= 93) return itemId - 39;
            if (itemId >= 311 && itemId <= 316) return itemId - 256;        // 55..60
            if (itemId >= 375 && itemId <= 380) return itemId - 314;        // 61..66
            switch (itemId)
            {
                case 552: return 73; case 553: return 78; case 554: return 75; case 555: return 76; case 556: return 74; case 557: return 77;
                case 558: return 79; case 559: return 83; case 560: return 81; case 561: return 82; case 562: return 80; case 563: return 84;
                case 1125: return 85; case 1126: return 86; case 1127: return 87; case 1128: return 88; case 1129: return 89; case 1130: return 90;
                case 844: return 91; case 841: return 92; case 842: return 93; case 839: return 94; case 843: return 95; case 840: return 96;
            }
            return -1;   // 547 (clone skills) and anything else: not supported offline
        }

        // Reuse Char.learnSkill's book mapping when school shops classify neutral-sys books.
        public static int BookClass(GameWorld world, int itemId)
        {
            var skill = world.FindSkillTemplate(BookToTemplate(itemId));
            return skill == null ? 0 : skill.ClassId;
        }

        private static int[] Find(PlayerState me, int templateId)
        {
            foreach (var s in me.Progress.Skills) if (s[0] == templateId) return s;
            return null;
        }

        // the `skill` row a learned skill currently uses
        public static SkillData Row(GameWorld world, int[] learned) { return world.FindSkill(learned[0], learned[1]); }

        /// <summary>Char.saveRms: the client saves its skill bars (5 screen slots, 3 hot keys, current skill).
        /// Only learned skills are kept.</summary>
        public static void SaveRms(Message m, PlayerState me)
        {
            string key = m.Reader.ReadUTF();
            int len = m.Reader.ReadInt();
            if (len < 0 || len > m.Reader.Remaining) return;
            var ab = new sbyte[len];
            for (int i = 0; i < len; i++) ab[i] = m.Reader.ReadSByte();
            switch (key)
            {
                case "KSkill": me.Progress.KSkill = Known(me, ab, 3); break;
                case "OSkill": me.Progress.OSkill = Known(me, ab, 5); break;
                case "CSkill": me.Progress.CSkill = ab; break;
                default: return;
            }
            PlayerItems.Save(me);
        }

        private static sbyte[] Known(PlayerState me, sbyte[] ab, int slots)
        {
            var a = new sbyte[slots];
            for (int i = 0; i < slots; i++)
                a[i] = i < ab.Length && ab[i] != -1 && Find(me, ab[i]) != null ? ab[i] : (sbyte)-1;
            return a;
        }

        // Char.learnSkill
        public static void LearnSkill(PlayerState me, GameWorld world, PlayerItem item)
        {
            int templateId = BookToTemplate(item.Id);
            var tpl = templateId < 0 ? null : world.FindSkillTemplate(templateId);
            if (tpl == null || tpl.ClassId != me.Record.ClassId || world.FindSkill(templateId, 1) == null)
            {
                PlayerService.ServerDialog(me, "Sách này không đúng với trường bạn!");
                return;
            }
            if (Find(me, templateId) != null) { PlayerService.ServerDialog(me, "Chiêu này đã học!"); return; }
            var learned = new[] { templateId, 1 };
            me.Progress.Skills.Add(learned);
            var row = Row(world, learned);
            PlayerService.UseBookSkill(me, item.Index, row.Id);
            PlayerService.LoadSkill(me, world);
            PlayerService.SkillBars(me);   // Char.useBookSkill: sendSkillShortcut
            if (me.SelectedSkill == null && tpl.Type == TypeAttack) Select(me, world, templateId);
            else PlayerCombat.SetAbility(me, world);
            PlayerTasks.OnLearnBook(me);
            PlayerItems.RemoveItem(me, item.Index, item.Quantity, false);
        }

        // Char.upSkill (sub -108): raise a learned skill by `point` levels using skill points
        public static void UpSkill(Message m, PlayerState me, GameWorld world)
        {
            if (me.IsDead) return;
            int templateId = (ushort)m.Reader.ReadShort();
            int point = m.Reader.ReadByte();
            if (point > me.SkillPoint || point < 0) return;
            var learned = Find(me, templateId);
            var tpl = world.FindSkillTemplate(templateId);
            if (learned == null || tpl == null) return;
            if (learned[1] == tpl.MaxPoint) { PlayerService.ServerDialog(me, "Kỹ năng đã đạt cấp tối đa."); return; }
            if (learned[1] + point > tpl.MaxPoint) { PlayerService.ServerDialog(me, "Kỹ năng chỉ có thể lên cấp tối đa là " + tpl.MaxPoint); return; }
            if (templateId >= 67 && templateId <= 72) { PlayerService.ServerDialog(me, "Không thể nâng cấp kĩ năng này"); return; }
            var next = world.FindSkill(templateId, learned[1] + point);
            if (next == null) return;
            if (next.Level > me.Level) { PlayerService.ServerDialog(me, "Trình độ không đủ yêu cầu."); return; }
            learned[1] += point;
            me.SkillPoint -= point;
            if (me.SelectedSkill != null && me.SelectedSkill.TemplateId == templateId) me.SelectedSkill = next;
            PlayerCombat.SetAbility(me, world);
            PlayerTasks.OnSkillUp(me);
            PlayerService.LoadSkill(me, world);
            PlayerService.SkillBars(me);   // Char.upSkill: sendSkillShortcut
            PlayerService.UpdateHp(me);
            PlayerService.UpdateMp(me);
            PlayerItems.Save(me);
        }

        // Char.upPotential (sub -109): spend potential points on strength / agility / HP / MP (index 0..3)
        public static void UpPotential(Message m, PlayerState me, GameWorld world)
        {
            if (me.IsDead) return;
            int index = m.Reader.ReadByte();
            int point = (ushort)m.Reader.ReadShort();
            if (point <= 0 || index < 0 || index > 3) return;
            if (point > me.PotentialPoint) { PlayerService.ServerDialog(me, "Bạn không đủ điểm."); return; }
            me.Potential[index] += point;
            me.PotentialPoint -= point;
            PlayerCombat.SetAbility(me, world);
            PlayerTasks.OnPotentialUp(me);
            PlayerService.UpdatePotential(me);
            PlayerService.UpdateHp(me);
            PlayerService.UpdateMp(me);
            PlayerItems.Save(me);
        }

        // Char.selectSkill(Message) (cmd 41)
        public static void SelectSkill(Message m, PlayerState me, GameWorld world)
        {
            int templateId = m.Reader.ReadShort();
            Select(me, world, templateId);
        }

        // Char.selectSkill(short): the chosen skill if it can be used to fight, otherwise the first attack skill
        public static void Select(PlayerState me, GameWorld world, int templateId)
        {
            SkillData pick = null, firstAttack = null;
            foreach (var s in me.Progress.Skills)
            {
                var row = Row(world, s);
                var tpl = world.FindSkillTemplate(s[0]);
                if (row == null || tpl == null || !CanFight(tpl, s[1])) continue;
                if (s[0] == templateId) pick = row;
                if (firstAttack == null && tpl.Type == TypeAttack) firstAttack = row;
            }
            var chosen = pick ?? firstAttack;
            if (chosen == null) return;
            me.SelectedSkill = chosen;
            PlayerCombat.SetAbility(me, world);
        }

        // Char.vSkillFight membership: clickable skills that have a point (or no point system)
        private static bool CanFight(SkillTemplateData tpl, int point)
        {
            bool clickable = tpl.Type == TypeAttack || tpl.Type == TypeLive || tpl.Type == TypeBuff || tpl.Type == TypeNpc;
            return clickable && (tpl.MaxPoint == 0 || point > 0);
        }

        // Char.vSupportSkill: passive skills whose options always count
        public static void AddSupportOptions(PlayerState me, GameWorld world, int[] support)
        {
            foreach (var s in me.Progress.Skills)
            {
                var tpl = world.FindSkillTemplate(s[0]);
                if (tpl == null || tpl.Type != TypeAuto) continue;
                var row = Row(world, s);
                if (row == null) continue;
                for (int i = 0; i < row.OptionIds.Length; i++)
                    if (row.OptionIds[i] >= 0 && row.OptionIds[i] < support.Length) support[row.OptionIds[i]] += row.OptionParams[i];
            }
        }
    }
}
