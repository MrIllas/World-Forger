using UnityEngine;
using System.IO;

[System.Serializable]
public class WorldForgerSettings : ScriptableObject
{
    private static WorldForgerSettings _instance;

    public int chunkSize = 30;
    public Vector2 cellSize = new Vector2(1, 1);
    public Material terrainMaterial;

    public float slopeThreshold = 0.5f;
    public float heightSnap = 0.5f;

    public static WorldForgerSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<WorldForgerSettings>("WorldForgerSettings");
                if (_instance == null)
                {
                    _instance = CreateInstance<WorldForgerSettings>();
#if UNITY_EDITOR
                    UnityEditor.AssetDatabase.CreateAsset(_instance, "Assets/Resources/WorldForgerSettings.asset");
                    UnityEditor.AssetDatabase.SaveAssets();
#endif
                }
            }
            return _instance;
        }
    }
}
