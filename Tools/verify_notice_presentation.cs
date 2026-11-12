// Run through Unity script eval. Isolated HUD state; restores every field/config.
var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var control = new UnityEditor.SerializedObject(NsoGameControl.Active);
var smooth = control.FindProperty("_smoothEffects");
bool originalSmooth = smooth.boolValue;
int checks = 0;
System.Action<bool, string> check = (ok, label) => { if (!ok) throw new System.Exception(label); checks++; };
try
{
    smooth.boolValue = true; control.ApplyModifiedPropertiesWithoutUndo();
    foreach (var type in new[] { typeof(Info), typeof(InfoMe) })
    {
        var fields = type.GetFields(flags);
        var saved = new System.Collections.Generic.Dictionary<System.Reflection.FieldInfo, object>();
        foreach (var field in fields) if (!field.IsLiteral && !field.IsInitOnly) saved[field] = field.GetValue(null);
        System.Action<string, object> set = (name, value) => type.GetField(name, flags).SetValue(null, value);
        System.Func<string, int> get = name => (int)type.GetField(name, flags).GetValue(null);
        var paint = type.GetMethod("PaintX", flags);
        System.Func<float, float> sample = t => (float)paint.Invoke(null, new object[] { t });
        var update = type.GetMethod("update", flags);
        var capture = type.GetMethod("CapturePaintState", flags);
        try
        {
            set("info", new InfoItem("notice regression", null, 20));
            set("x", 100); set("limLeft", 2); set("strWidth", 20); set("p1", 3); set("p2", 0);
            capture.Invoke(null, null); update.Invoke(null, null);
            int end = get("x");
            check(end == 94 || end == 98, type.Name + " keeps original exit step");
            var positions = new System.Collections.Generic.HashSet<float>();
            foreach (float t in new[] { 0f, .25f, .5f, .75f, 1f })
            {
                float value = sample(t); positions.Add(value);
                check(UnityEngine.Mathf.Abs(value - UnityEngine.Mathf.Lerp(100, end, t)) < .001f, type.Name + " fractional exit " + t);
            }
            check(positions.Count == 5 && get("x") == end, type.Name + " five render positions without logic mutation");
            capture.Invoke(null, null);
            check(sample(.1f) == end && sample(.9f) == end, type.Name + " paused tick never oscillates");
            set("p1", 2); set("p2", 0); capture.Invoke(null, null); update.Invoke(null, null);
            check(sample(.1f) == end && sample(.9f) == end && get("p2") == 1, type.Name + " held notice and timer unchanged");
            set("p1", 0); set("x", 100); capture.Invoke(null, null); update.Invoke(null, null);
            check(sample(.5f) > get("x") && sample(.5f) < 100, type.Name + " entrance interpolation");
            smooth.boolValue = false; control.ApplyModifiedPropertiesWithoutUndo();
            check(sample(.25f) == get("x"), type.Name + " opt out");
            smooth.boolValue = true; control.ApplyModifiedPropertiesWithoutUndo();
            set("info", new InfoItem("replacement", null, 20)); set("x", 400);
            check(sample(.25f) == 400, type.Name + " replacement never slides from previous text");
            set("info", null);
            check(sample(.25f) == 400, type.Name + " empty notice safe");
        }
        finally { foreach (var pair in saved) pair.Key.SetValue(null, pair.Value); }
    }
}
finally { smooth.boolValue = originalSmooth; control.ApplyModifiedPropertiesWithoutUndo(); }
return "Notice presentation: " + checks + "/" + checks + " PASS";
