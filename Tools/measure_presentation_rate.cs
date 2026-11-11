// Play mode, in a map. Samples actual Repaint/logic counters at 60 and 120 FPS caps.
// Asynchronous: read %TEMP%/nso-presentation-rate.txt after about six seconds.
if (!UnityEditor.EditorApplication.isPlaying || GameCanvas.currentScreen != GameScr.instance)
    throw new System.InvalidOperationException("Enter a map first.");
var control=NsoGameControl.Active;int oldCap=control.targetFrameRate;
var paint=typeof(Main).GetField("paintCount",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
var logic=typeof(Main).GetField("updateCount",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
int phase=0,startPaint=0,startLogic=0;bool sampling=false;
double began=UnityEditor.EditorApplication.timeSinceStartup;
var result=new System.Text.StringBuilder();
string output=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"nso-presentation-rate.txt");
control.targetFrameRate=60;
UnityEditor.EditorApplication.CallbackFunction callback=null;
callback=()=>{
    try {
        if(!UnityEditor.EditorApplication.isPlaying || Main.main==null)throw new System.Exception("Play ended during sampling.");
        double now=UnityEditor.EditorApplication.timeSinceStartup;
        if(!sampling) {
            if(now-began<0.5)return;
            sampling=true;began=now;startPaint=(int)paint.GetValue(Main.main);startLogic=(int)logic.GetValue(Main.main);return;
        }
        if(now-began<2)return;
        double elapsed=now-began;
        result.AppendLine("cap="+control.targetFrameRate+", actual paints/s="+(((int)paint.GetValue(Main.main)-startPaint)/elapsed).ToString("F1")+", logic/s="+(((int)logic.GetValue(Main.main)-startLogic)/elapsed).ToString("F1"));
        if(phase++==0){control.targetFrameRate=120;sampling=false;began=now;return;}
        UnityEditor.EditorApplication.update-=callback;control.targetFrameRate=oldCap;
        System.IO.File.WriteAllText(output,result.ToString());
    } catch(System.Exception e) {
        UnityEditor.EditorApplication.update-=callback;
        if(control!=null)control.targetFrameRate=oldCap;
        System.IO.File.WriteAllText(output,result+"ERROR: "+e.Message);
    }
};
UnityEditor.EditorApplication.update+=callback;
return "Measuring actual render/logic throughput; report: "+output;
