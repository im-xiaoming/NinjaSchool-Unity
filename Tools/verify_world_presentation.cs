// Play-mode, in-map regression. Isolated objects, no gameplay ticks/packets/save changes.
if (!UnityEditor.EditorApplication.isPlaying || GameCanvas.currentScreen != GameScr.instance)
    throw new System.InvalidOperationException("Enter a map first.");
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var remember=typeof(Interp).GetMethod("Remember",flags);
var control=new UnityEditor.SerializedObject(NsoGameControl.Active);
var enabled=control.FindProperty("_smoothWorldMotion");bool wasEnabled=enabled.boolValue;
var effects=control.FindProperty("_smoothEffects");bool wereEffects=effects.boolValue;
int count=0;
System.Action<bool,string> check=(ok,label)=>{if(!ok)throw new System.Exception("FAIL: "+label);count++;};
System.Func<float,float,bool> near=(a,b)=>UnityEngine.Mathf.Abs(a-b)<0.001f;
System.Action<object,string,int> put=(o,name,value)=>{var f=o.GetType().GetField(name);f.SetValue(o,System.Convert.ChangeType(value,f.FieldType));};
System.Func<object,string,int> get=(o,name)=>System.Convert.ToInt32(o.GetType().GetField(name).GetValue(o));
System.Type[] types={typeof(Char),typeof(Mob),typeof(ItemMap),typeof(Arrow),typeof(MonsterDart),typeof(Domsang),typeof(MobSoul),typeof(Lanterns),typeof(BuNhin),typeof(EggMonters)};
string[] xs={"cx","x","x","ax","x","xS","x","x","x","x"};
string[] ys={"cy","y","y","ay","y","yS","y","y","y","y"};
int cameraX=GameScr.cmx,cameraY=GameScr.cmy;
try {
    enabled.boolValue=true;effects.boolValue=true;control.ApplyModifiedPropertiesWithoutUndo();
    for(int i=0;i<types.Length;i++) {
        var o=System.Runtime.Serialization.FormatterServices.GetUninitializedObject(types[i]);
        put(o,xs[i],100);put(o,ys[i],200);
        Interp.BeforeTick();remember.Invoke(null,new[]{o});put(o,xs[i],101);put(o,ys[i],199);
        var samples=new System.Collections.Generic.HashSet<float>();
        foreach(float t in new[]{0.1f,0.3f,0.5f,0.7f,0.9f}) {
            Interp.BeginPaint(t);var offset=Interp.ObjectOffset(o);
            samples.Add(get(o,xs[i])+offset.x);
            check(near(get(o,xs[i])+offset.x,100+t) && near(get(o,ys[i])+offset.y,200-t),types[i].Name+" fractional sample "+t);
            Interp.EndPaint();
        }
        check(samples.Count==5,types[i].Name+" slow one-pixel motion has five distinct positions");
        check(get(o,xs[i])==101 && get(o,ys[i])==199,types[i].Name+" authoritative coordinates restored");
        Interp.BeforeTick();remember.Invoke(null,new[]{o});put(o,xs[i],500);
        Interp.BeginPaint(0.25f);check(Interp.ObjectOffset(o)==UnityEngine.Vector2.zero && get(o,xs[i])==500,types[i].Name+" teleport snaps");Interp.EndPaint();
    }
    Interp.BeforeTick();GameScr.cmx=cameraX+1;GameScr.cmy=cameraY-1;
    Interp.BeginPaint(0.25f);
    check(near(GameScr.cmx-Interp.CameraOffset.x,cameraX+0.25f) && near(GameScr.cmy-Interp.CameraOffset.y,cameraY-0.25f),"camera fractional offset has the world-translation sign");
    Interp.EndPaint();check(GameScr.cmx==cameraX+1 && GameScr.cmy==cameraY-1,"camera logic coordinates restored");
    var c=new Char{cx=100,cy=100};Interp.BeforeTick();remember.Invoke(null,new object[]{c});c.cx++;
    enabled.boolValue=false;control.ApplyModifiedPropertiesWithoutUndo();Interp.BeginPaint(0.4f);
    check(Interp.ObjectOffset(c)==UnityEngine.Vector2.zero && Interp.CameraOffset==UnityEngine.Vector2.zero,"world toggle removes fractional transforms");Interp.EndPaint();
    check(c.cx==101,"toggle preserves logic coordinates");
    var scroll=new Scroll{width=100,height=100,nITEM=20,cmx=0,cmy=10};Scroll.CaptureAllPaintStates();scroll.cmy=11;
    check(near(scroll.PaintOffset(0.25f).y,0.75f),"scroll content moves fractionally without changing its logical offset");
    check(scroll.cmy==11,"scroll hit-testing offset unchanged");
    Scroll.CaptureAllPaintStates();check(scroll.PaintOffset(0.25f)==UnityEngine.Vector2.zero,"paused scroll does not oscillate");
    scroll.cmy=12;scroll.nITEM++;check(scroll.PaintOffset(0.25f)==UnityEngine.Vector2.zero,"changed list geometry snaps");
    scroll.clear();check(scroll.PaintOffset(0.25f)==UnityEngine.Vector2.zero,"recycled scroll has no stale motion");
    var graphics=new mGraphics();
    var offsetField=typeof(mGraphics).GetField("scrollPaintOffset",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
    graphics.SetScrollPaintOffset(0.25f,0.5f);
    check((UnityEngine.Vector2)offsetField.GetValue(graphics)==new UnityEngine.Vector2(0.25f,0.5f)*mGraphics.zoomLevel,"scroll retains fractional device-pixel placement before clipping");
    graphics.setClip(2,3,100,80);check((UnityEngine.Vector2)offsetField.GetValue(graphics)==UnityEngine.Vector2.zero,"new viewport clears previous content offset");
    graphics.translate(-10,-20);graphics.SetScrollPaintOffset(0.25f,0.5f);graphics.translate(10,20);
    check((UnityEngine.Vector2)offsetField.GetValue(graphics)==UnityEngine.Vector2.zero,"origin reset clears previous content offset");
    graphics.SetScrollPaintOffset(0.25f,0.5f);graphics.reset();
    check((UnityEngine.Vector2)offsetField.GetValue(graphics)==UnityEngine.Vector2.zero,"paint reset clears previous content offset");
    int oldCap=NsoGameControl.Active.targetFrameRate;
    try {NsoGameControl.Active.targetFrameRate=30;check(NsoGameControl.Active.targetFrameRate==60,"render cap cannot be configured below 60");}
    finally {NsoGameControl.Active.targetFrameRate=oldCap;}
} finally {
    Interp.EndPaint();GameScr.cmx=cameraX;GameScr.cmy=cameraY;
    enabled.boolValue=wasEnabled;effects.boolValue=wereEffects;control.ApplyModifiedPropertiesWithoutUndo();Interp.BeforeTick();
}
return count+" world/camera/scroll presentation checks passed";
