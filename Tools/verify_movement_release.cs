// Run with unity command script eval --file Tools/verify_movement_release.cs, in Play mode in a map.
// Uses isolated characters and restores input/singleton/config; no packets or save edits.
if (!UnityEditor.EditorApplication.isPlaying || GameCanvas.currentScreen != GameScr.instance)
    throw new System.InvalidOperationException("Enter a map before running movement checks.");
var actual=Char.getMyChar();
var singleton=typeof(Char).GetField("myChar",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
var holds=(bool[])GameCanvas.keyHold.Clone();
var releases=(bool[])GameCanvas.keyReleasedz.Clone();
int ascii=GameCanvas.keyAsciiPress, count=0;
var so=new UnityEditor.SerializedObject(NsoGameControl.Active);
var setting=so.FindProperty("_instantMovementStop"); bool oldSetting=setting.boolValue;
System.Action<bool,string> check=(ok,label)=>{if(!ok)throw new System.Exception("FAIL: "+label);count++;};
System.Func<Char> running=()=>{
    System.Array.Clear(GameCanvas.keyHold,0,GameCanvas.keyHold.Length);
    var c=new Char {me=true,statusMe=2,cvx=8,cBonusSpeed=2,cx=500,cy=240};
    singleton.SetValue(null,c);return c;
};
try {
    setting.boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
    int[] keys={-3,-4,52,54,97,100};int[] slots={23,22,Key.NUM4,Key.NUM6,18,19};
    for(int i=0;i<keys.Length;i++) {
        var c=running();c.cvx=(i==0||i==2)?-8:8;GameCanvas.keyHold[slots[i]]=true;
        Main.canvas.keyReleased(keys[i]);
        check(c.cvx==0 && c.statusMe==1 && c.cBonusSpeed==0 && c.cx==500,"release stops alias "+keys[i]+" without an extra step");
    }
    var held=running();GameCanvas.keyHold[22]=true;GameCanvas.keyHold[23]=true;Main.canvas.keyReleased(-3);
    check(held.cvx==8 && held.statusMe==2,"opposite direction still held continues");
    Main.canvas.keyReleased(-4);check(held.cvx==0 && held.statusMe==1,"last held direction stops");
    var alias=running();GameCanvas.keyHold[22]=true;GameCanvas.keyHold[Key.NUM6]=true;Main.canvas.keyReleased(-4);
    check(alias.cvx==8,"another horizontal alias still held continues");
    var unrelated=running();Main.canvas.keyReleased(113);
    check(unrelated.cvx==8,"unrelated key release cannot stop movement");
    var notHeld=running();Main.canvas.keyReleased(-4);
    check(notHeld.cvx==8,"release without owned held input cannot stop movement");
    foreach(int state in new[]{3,4,14}) {
        var c=running();c.statusMe=state;c.cvy=-6;GameCanvas.keyHold[22]=true;Main.canvas.keyReleased(-4);
        check(c.cvx==8 && c.cvy==-6 && c.statusMe==state,"non-running state preserved: "+state);
    }
    var path=running();path.currentMovePoint=new MovePoint(700,240);GameCanvas.keyHold[22]=true;Main.canvas.keyReleased(-4);
    check(path.cvx==8 && path.currentMovePoint!=null,"destination movement preserved");
    var remote=running();remote.me=false;remote.StopGroundMovementOnRelease();
    check(remote.cvx==8 && remote.statusMe==2,"remote character preserved");
    var touch=running();touch.StopGroundMovementOnRelease();
    check(touch.cvx==0 && touch.statusMe==1,"pointer-cleared horizontal controls stop ground motion");
    var legacy=running();setting.boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
    GameCanvas.keyHold[22]=true;Main.canvas.keyReleased(-4);
    check(legacy.cvx==8 && legacy.statusMe==2,"Inspector opt-out retains original inertia");
} finally {
    singleton.SetValue(null,actual);System.Array.Copy(holds,GameCanvas.keyHold,holds.Length);
    System.Array.Copy(releases,GameCanvas.keyReleasedz,releases.Length);GameCanvas.keyAsciiPress=ascii;
    setting.boolValue=oldSetting;so.ApplyModifiedPropertiesWithoutUndo();
}
return count+"/18 movement release checks passed";
