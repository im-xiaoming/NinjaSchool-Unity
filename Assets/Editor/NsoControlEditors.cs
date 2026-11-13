using System.IO;
using UnityEditor;
using UnityEngine;

// Inspector buttons for the "NSO ..." Hierarchy objects.
[CustomEditor(typeof(NsoInputControl))]
class NsoInputControlEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        NsoInputControl t = (NsoInputControl)target;
        EditorGUILayout.Space();
        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Press Play to use the buttons.", MessageType.Info);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Up", GUILayout.Width(70)))
                t.Press(NsoInputControl.Up);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Left"))
                t.Press(NsoInputControl.Left);
            if (GUILayout.Button("OK / Enter"))
                t.Press(NsoInputControl.Enter);
            if (GUILayout.Button("Right"))
                t.Press(NsoInputControl.Right);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Down", GUILayout.Width(70)))
                t.Press(NsoInputControl.Down);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Soft key left (F1)"))
                t.Press(NsoInputControl.SoftLeft);
            if (GUILayout.Button("Soft key right (F2)"))
                t.Press(NsoInputControl.SoftRight);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Click at pixel"))
                t.Click();
        }
    }
}

[CustomEditor(typeof(NsoServerControl))]
class NsoServerControlEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(Application.isPlaying))
            DrawPropertiesExcluding(serializedObject, "status", "address", "dataFolder", "wikiUrl");
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("status"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("address"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dataFolder"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wikiUrl"));
        }

        serializedObject.ApplyModifiedProperties();
        // dev wiki: running only while playing
        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(ServerBootstrap.WikiUrl)))
            if (GUILayout.Button("Open dev wiki in browser"))
                Application.OpenURL(ServerBootstrap.WikiUrl);
        string folder;
        try
        {
            folder = ((NsoServerControl)target).ConfiguredDataFolder;
        }
        catch (System.Exception ex)
        {
            EditorGUILayout.HelpBox(ex.Message, MessageType.Error);
            return;
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Open save-data folder"))
        {
            Directory.CreateDirectory(folder);
            EditorUtility.RevealInFinder(folder);
        }

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Delete save data (new game state)") && EditorUtility.DisplayDialog("Delete save data", "Delete all accounts and characters in\n" + folder + "\n\nThis cannot be undone.", "Delete", "Cancel"))
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
                Debug.Log("[NSO] Deleted " + folder);
            }
        }

        if (Application.isPlaying)
            EditorGUILayout.HelpBox("Stop Play mode before deleting save data.", MessageType.Info);
    }
}

static class NsoControlMenu
{
    // Adds (or re-adds) the control objects to the open scene.
    [MenuItem("Tools/NSO/Create Control Objects")]
    static void Create()
    {
        Ensure<NsoGameControl>("NSO Game Control");
        Ensure<NsoInputControl>("NSO Input Control");
        Ensure<NsoServerControl>("NSO Server Control");
    }

    static void Ensure<T>(string name)
        where T : Component
    {
        if (UnityEngine.Object.FindFirstObjectByType<T>() != null)
            return;
        GameObject go = new GameObject(name);
        go.AddComponent<T>();
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
    }
}
