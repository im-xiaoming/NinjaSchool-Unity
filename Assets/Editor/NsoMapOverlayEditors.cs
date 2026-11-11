using NsoBridge;
using UnityEditor;
using UnityEngine;

// Inspector for map overlay objects: placement buttons only change the overlay component / Transform (with Undo),
// never the game's state.
[CustomEditor(typeof(NsoMapObject)), CanEditMultipleObjects]
class NsoMapObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        NsoMapOverlayBridge bridge = FindBridge();
        EditorGUILayout.Space();
        if (bridge == null)
        {
            EditorGUILayout.HelpBox("No NSO Map Overlay in the scene: add the NsoMapOverlayBridge component to an object.", MessageType.Warning);
            return;
        }
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture Scene Position"))
            {
                foreach (Object t in targets)
                {
                    NsoMapObject o = (NsoMapObject)t;
                    Undo.RecordObject(o, "Capture Scene Position");
                    Vector2 p = bridge.EditToMap(o.transform.position);
                    o.position = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
                    PrefabUtility.RecordPrefabInstancePropertyModifications(o);
                }
            }
            if (GUILayout.Button("Apply Logical Position"))
            {
                foreach (Object t in targets)
                {
                    NsoMapObject o = (NsoMapObject)t;
                    Undo.RecordObject(o.transform, "Apply Logical Position");
                    o.transform.position = bridge.EditPosition(o.position);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        if (Application.isPlaying)
            EditorGUILayout.HelpBox("While playing, the overlay moves this object with the game camera. Placement buttons work outside Play mode.", MessageType.Info);
    }

    private static NsoMapOverlayBridge FindBridge()
    {
        return NsoMapOverlayBridge.Active != null ? NsoMapOverlayBridge.Active : Object.FindFirstObjectByType<NsoMapOverlayBridge>();
    }
}

[CustomEditor(typeof(NsoMapOverlayBridge))]
class NsoMapOverlayBridgeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "ready", "currentMapId", "currentZone", "cameraScroll", "zoom");
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ready"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentMapId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("currentZone"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraScroll"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("zoom"));
        }
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Registered objects", NsoMapOverlayBridge.Objects.Count.ToString());
        if (Application.isPlaying) Repaint();   // live runtime fields
    }
}
