// Offline deviation (user request 2026-11-12): in the data the basic attack ("Chiêu cơ bản", skill template 0, the only
// skill of a character without a school) has a single row (point 0, cooldown 500 ms) and never grows. Offline, it grows
// with the level while the character has no school: one point per level after level 1, each point a little faster.
// The points are spent automatically; after joining a school the normal skill points take over (the school clears the
// basic skill). Shared by the client (Controller.createSkill adds the extra rows) and the offline server
// (GameWorld.LoadSkills adds the same rows; PlayerCombat syncs the point on level-up), so both use the same ids/cooldowns.
public static class BasicSkillGrowth
{
    /// <summary>Skill template that grows (the basic attack of class 0).</summary>
    public const int TemplateId = 0;
    /// <summary>Highest point (level 10 = 9 points, the level at which a school can be joined).</summary>
    public static int MaxPoint = 9;
    /// <summary>Cooldown removed per point, in milliseconds.</summary>
    public static int CooldownStepMs = 20;
    /// <summary>Cooldown never goes below this, in milliseconds.</summary>
    public static int MinCooldownMs = 100;
    /// <summary>Skill row id of point p is IdBase + p (p >= 1). Real ids end at 966; skill ids are shorts.</summary>
    public const int IdBase = 30000;
    /// <summary>Point the basic attack has at this character level (class 0 only).</summary>
    public static int PointForLevel(int level)
    {
        return System.Math.Max(0, System.Math.Min(MaxPoint, level - 1));
    }

    /// <summary>Cooldown of the given point, from the cooldown of the original point-0 row.</summary>
    public static int Cooldown(int baseCooldown, int point)
    {
        return System.Math.Max(MinCooldownMs, baseCooldown - CooldownStepMs * point);
    }

    /// <summary>Max point shown in the skill panel. The template keeps max_point 0 (so point 0 can still fight and
    /// points cannot be added by hand); the panel shows the grown maximum instead.</summary>

#if UNITY_5_3_OR_NEWER // Presentation-only: keep the shared progression usable by the headless server trainer.

    public static int DisplayMaxPoint(SkillTemplate tpl)
    {
        return tpl.id == TemplateId && tpl.maxPoint == 0 && tpl.skills != null && tpl.skills.Length > 1 ? tpl.skills.Length - 1 : tpl.maxPoint;
    }

#endif
    /// <summary>Skill row id of a point (point 0 keeps its original id).</summary>
    public static short SkillId(int originalId, int point)
    {
        return (short)(point <= 0 ? originalId : IdBase + point);
    }
}
