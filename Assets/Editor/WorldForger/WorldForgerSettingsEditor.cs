using UnityEditor;
using UnityEngine;

public class PolyxWorldConfigEditor : EditorWindow
{
    private WorldForgerSettings _settings;
    private SerializedObject serializedConfig;

    [MenuItem("Nova Dot/World Forger/World Forger Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow<PolyxWorldConfigEditor>("World Forger Settings");
        window.LoadConfig();
    }

    private void LoadConfig()
    {
        _settings = WorldForgerSettings.Instance;
        serializedConfig = new SerializedObject(_settings);
    }

    private void OnGUI()
    {
        if (_settings == null)
        {
            LoadConfig();
        }

        serializedConfig.Update();

        EditorGUILayout.LabelField("World Forger Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("chunkSize"));
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("cellSize"));
        EditorGUILayout.PropertyField(serializedConfig.FindProperty("terrainMaterial"));
        //EditorGUILayout.PropertyField(serializedConfig.FindProperty("slopeThreshold"));
        //EditorGUILayout.PropertyField(serializedConfig.FindProperty("heightSnap"));

        serializedConfig.ApplyModifiedProperties();
    }
}