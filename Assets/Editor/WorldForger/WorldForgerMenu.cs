using UnityEngine;
using UnityEditor;

public static class WorldForgerMenu
{
    [MenuItem("GameObject/Nova Dot/World Forger/Terrain", false, 10)]
    public static void CreateWorldTerrain()
    {
        // Create a new GameObject named "World Terrain"
        GameObject worldTerrain = new GameObject("World Terrain");

        // Ensures to always be generated at the correct position, angle and scale
        worldTerrain.transform.position = Vector3.zero;
        worldTerrain.transform.rotation = Quaternion.identity;
        worldTerrain.transform.localScale = Vector3.one;

        // Add the PolyxWorldMarcher component
        worldTerrain.AddComponent<WorldForger>();

        // Give it the Layer terrain (3) (IMPORTANT - NEVER CHANGE)
        worldTerrain.layer = 3;

        // Register this action for Undo (Ctrl+Z support)
        Undo.RegisterCreatedObjectUndo(worldTerrain, "Create World Terrain");

        // Select the new GameObject in the Hierarchy
        Selection.activeGameObject = worldTerrain;
    }
}