// Run through `unity command script eval --file Tools/verify_effect_presentation.cs` in Play mode, in a map.
// Synthetic checks restore shared menu/effect arrays and do not send game packets or modify saves.
if (!UnityEditor.EditorApplication.isPlaying || GameCanvas.currentScreen != GameScr.instance)
    throw new System.InvalidOperationException("Enter a map before running presentation checks.");
int count = 0;
System.Action<bool,string> check=(ok,label)=>{if(!ok)throw new System.Exception("FAIL: "+label);count++;};
System.Func<float,float,bool> near=(a,b)=>UnityEngine.Mathf.Abs(a-b)<0.0001f;
var tween=new NsoHealthTween();
check(near(tween.Sample(1f,0,0.12f),1f),"initial HP snaps to actual ratio");
check(near(tween.Sample(0.5f,0,0.12f),1f),"damage begins from the visible old HP");
check(near(tween.Sample(0.5f,0.06,0.12f),0.75f),"HP has a fractional midpoint");
check(near(tween.Sample(0.5f,0.12,0.12f),0.5f),"HP reaches target on time");
check(near(tween.Sample(1f,0.13,0.12f),1f),"respawn/full healing resets old HP immediately");
var chained=new NsoHealthTween();chained.Sample(1,0,0.12f);chained.Sample(0.8f,0,0.12f);
float before=chained.Sample(0.8f,0.06,0.12f);
check(near(chained.Sample(0.4f,0.06,0.12f),before),"successive damage retargets without jumping");
check(near(chained.Sample(0.4f,0.18,0.12f),0.4f),"successive damage reaches latest HP");
check(near(chained.Sample(0.2f,0.19,0),0.2f),"zero duration is instant");
var lowRate=new NsoHealthTween();var highRate=new NsoHealthTween();
lowRate.Sample(1,0,0.12f); highRate.Sample(1,0,0.12f);
lowRate.Sample(0.5f,0,0.12f); highRate.Sample(0.5f,0,0.12f);
for(int i=1;i<8;i++)highRate.Sample(0.5f,i/120.0,0.12f);
check(near(lowRate.Sample(0.5f,0.08,0.12f),highRate.Sample(0.5f,0.08,0.12f)),"health duration is independent of render rate");
var so=new UnityEditor.SerializedObject(NsoGameControl.Active);var enabled=so.FindProperty("_smoothEffects");bool wasEnabled=enabled.boolValue;
try {
    enabled.boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
    var positions=new System.Collections.Generic.HashSet<float>();
    for(int i=0;i<5;i++)positions.Add(Interp.EffectPosition(100,98,(i+0.5f)/5f));
    check(positions.Count==5,"120 render / 24 logic offers five different damage-text positions per tick");
    check(near(Interp.EffectPosition(100,100,0.6f),100),"paused snapshots remain stationary");
    enabled.boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
    check(near(Interp.EffectPosition(100,98,0.5f),98),"Inspector switch restores original snapped drawing");
} finally { enabled.boolValue=wasEnabled;so.ApplyModifiedPropertiesWithoutUndo(); }
var oldY=Menu.menuTemY; int oldScroll=Menu.cmx;
try {
    var menu=new Menu();Menu.menuTemY=new[]{200,220};Menu.cmx=7;menu.CapturePaintState();
    Menu.menuTemY[0]=100;
    float drawn=menu.PaintRowY(0);
    check(drawn>=100 && drawn<=200,"menu draws between the previous and current logical rows");
    menu.CapturePaintState();
    check(near(menu.PaintRowY(0),100),"menu capture removes stale motion when frozen");
    var opened=typeof(Menu).GetField("paintOpenedAt",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    menu.menuY=100;Menu.menuTemY[1]=100;menu.CapturePaintState();opened.SetValue(menu,UnityEngine.Time.unscaledTimeAsDouble);
    check(near(menu.PaintRowY(0),GameCanvas.h),"recently reopened menu begins at the bottom even if logical rows snap");
    check(menu.isScrolling(),"opening menu cannot activate an invisible target-position button");
    opened.SetValue(menu,UnityEngine.Time.unscaledTimeAsDouble-NsoGameControl.MenuOpenSeconds*0.5);
    check(menu.PaintRowY(0)>100 && menu.PaintRowY(0)<GameCanvas.h,"reopened menu has a fractional opening midpoint");
} finally {Menu.menuTemY=oldY;Menu.cmx=oldScroll;GameCanvas.menu.CapturePaintState();}
return count+"/17 effect presentation checks passed";
