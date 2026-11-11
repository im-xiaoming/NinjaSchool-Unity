// Run with unity command script eval --file Tools/verify_double_click.cs, in Play mode in a map.
// Checks GameScr.onTargetClicked (double-click on a world target = Enter / keyPressedz[5]).
// Restores keyPressedz[5], the click state and the Inspector setting; no packets are sent.
if (!UnityEditor.EditorApplication.isPlaying || GameCanvas.currentScreen != GameScr.instance)
    throw new System.InvalidOperationException("Enter a map before running double-click checks.");
var F=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var gs=GameScr.instance;
var click=typeof(GameScr).GetMethod("onTargetClicked",F);
var fTarget=typeof(GameScr).GetField("lastClickTarget",F);
var fTime=typeof(GameScr).GetField("lastClickTime",F);
object oldTarget=fTarget.GetValue(gs); object oldTime=fTime.GetValue(gs); bool oldFire=GameCanvas.keyPressedz[5];
var so=new UnityEditor.SerializedObject(NsoGameControl.Active);
var setting=so.FindProperty("_doubleClickMs"); int oldMs=setting.intValue;
int count=0;
System.Action<bool,string> check=(ok,label)=>{if(!ok)throw new System.Exception("FAIL: "+label);count++;};
System.Action reset=()=>{fTarget.SetValue(gs,null);GameCanvas.keyPressedz[5]=false;};
object a=new object(), b=new object();
try {
    setting.intValue=400;so.ApplyModifiedPropertiesWithoutUndo();
    reset(); click.Invoke(gs,new[]{a}); check(!GameCanvas.keyPressedz[5],"single click does not press Enter");
    click.Invoke(gs,new[]{a}); check(GameCanvas.keyPressedz[5],"second click on same target presses Enter");
    GameCanvas.keyPressedz[5]=false; click.Invoke(gs,new[]{a}); check(!GameCanvas.keyPressedz[5],"third click starts a new pair");
    reset(); click.Invoke(gs,new[]{a}); click.Invoke(gs,new[]{b}); check(!GameCanvas.keyPressedz[5],"clicks on different targets do not press Enter");
    reset(); click.Invoke(gs,new[]{a}); fTime.SetValue(gs,mSystem.getCurrentTimeMillis()-1000L);
    click.Invoke(gs,new[]{a}); check(!GameCanvas.keyPressedz[5],"slow second click does not press Enter");
    setting.intValue=0;so.ApplyModifiedPropertiesWithoutUndo();
    reset(); click.Invoke(gs,new[]{a}); click.Invoke(gs,new[]{a}); check(!GameCanvas.keyPressedz[5],"0 ms disables the feature");
} finally {
    setting.intValue=oldMs;so.ApplyModifiedPropertiesWithoutUndo();
    fTarget.SetValue(gs,oldTarget); fTime.SetValue(gs,oldTime); GameCanvas.keyPressedz[5]=oldFire;
}
return "double-click checks passed: "+count+"/6";
