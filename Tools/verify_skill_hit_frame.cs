// Run in Editor Play mode after entering a map:
// unity command script eval --file Tools/verify_skill_hit_frame.cs --project-path "C:/My project"
// Synthetic remote characters exercise Char.updateSkillPaint without sending packets or changing the save.
if (!UnityEditor.EditorApplication.isPlaying || GameCanvas.currentScreen != GameScr.instance)
    throw new System.InvalidOperationException("Enter a map in Play mode before running this regression.");
var update = typeof(Char).GetMethod("updateSkillPaint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var paint = GameScr.sks[0];
int length = paint.skillStand.Length;
int hit = length - length / 3;
int passed = 0;
System.Action<bool, string> check = (ok, label) => { if (!ok) throw new System.Exception("FAIL: " + label); passed++; };
System.Func<Char> create = () => {
    var c = new Char();
    c.cx = Char.getMyChar().cx; c.cy = Char.getMyChar().cy;
    c.charFocus = new Char();
    c.attMobs = new Mob[0]; c.attChars = new Char[0];
    c.setAutoSkillPaint(paint, 0);
    return c;
};
var before = create(); before.indexSkill = hit - 1; update.Invoke(before, null);
check(before.attMobs != null, "no early hit");
var exact = create(); exact.indexSkill = hit; update.Invoke(exact, null);
check(exact.attMobs == null, "hit on original frame");
var skipped = create(); skipped.indexSkill = hit + 1; update.Invoke(skipped, null);
check(skipped.attMobs == null, "hit after render skips the exact frame");
skipped.attMobs = new Mob[0]; skipped.attChars = new Char[0]; update.Invoke(skipped, null);
check(skipped.attMobs != null, "no duplicate when logic sees the same animation frame twice");
skipped.setAutoSkillPaint(paint, 0); skipped.indexSkill = hit; update.Invoke(skipped, null);
check(skipped.attMobs == null, "new cast resets dispatch guard");
var ended = create(); ended.indexSkill = length; update.Invoke(ended, null);
check(ended.attMobs == null && ended.skillPaint == null, "late hit dispatches before animation cleanup");
var dead = create(); dead.indexSkill = hit; dead.statusMe = 14; update.Invoke(dead, null);
check(dead.attMobs != null, "dead character does not hit");
var projectile = create(); projectile.indexSkill = hit;
projectile.setAttack(); // same dispatch path as Arrow.endMe
projectile.attMobs = new Mob[0]; projectile.attChars = new Char[0];
update.Invoke(projectile, null);
check(projectile.attMobs != null, "projectile impact is not dispatched again by the next logic tick");
return passed + "/8 skill hit-frame regressions passed (animation length=" + length + ", hit frame=" + hit + ")";
