using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldForger))]
public class WorldForgerEditor : Editor
{
    [SerializeField] private int _currentHandle;
    bool showNoise = false;

    WorldForger _terrain;
    SerializedProperty selectedToolIndex;
    SerializedProperty tools;

    SerializedProperty sloopThreshold;
    //SerializedProperty lastTool;
    //SerializedProperty currentTool;

    //WorldForgerTool lastToolInstance;
    WorldForgerTool currentToolInstance;

    public void OnEnable()
    {
        _terrain = (WorldForger)target;
        selectedToolIndex = serializedObject.FindProperty("_selectedTool");
        tools = serializedObject.FindProperty("_tools");
        sloopThreshold = serializedObject.FindProperty("slopeThreshold");
        //lastTool = serializedObject.FindProperty("_lastTool");
        //currentTool = serializedObject.FindProperty("_currentTool");

        InitializeTools();

        //if (currentTool.objectReferenceValue != null)
        //{
        //    currentToolInstance = (WorldForgerTool)currentTool.objectReferenceValue;
        //    currentToolInstance.ToolSelected();
        //}

        serializedObject.ApplyModifiedProperties();
    }

    public void OnDisable()
    {
        
    }

    private void InitializeTools()
    {
        if (tools.arraySize == 0) tools.arraySize = 4;

        if (tools.GetArrayElementAtIndex(0).objectReferenceValue == null)
            tools.GetArrayElementAtIndex(0).objectReferenceValue = CreateInstance<WorldForgerTool>();

        if (tools.GetArrayElementAtIndex(1).objectReferenceValue == null)
            tools.GetArrayElementAtIndex(1).objectReferenceValue = CreateInstance<ChunkTool>();

        if (tools.GetArrayElementAtIndex(2).objectReferenceValue == null)
            tools.GetArrayElementAtIndex(2).objectReferenceValue = CreateInstance<TerrainBrushTool>();

        if (tools.GetArrayElementAtIndex(3).objectReferenceValue == null)
            tools.GetArrayElementAtIndex(3).objectReferenceValue = CreateInstance<TextureTool>();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        //Options
        EditorGUILayout.PropertyField(sloopThreshold);

        // Tool Menu
        selectedToolIndex.intValue = GUILayout.Toolbar(selectedToolIndex.intValue,
            new string[] {
                "None",
                "Chunk Tool",
                "Terrain Brush",
                "Texture Tool"
            }
         );

        // Handle Tool Switch
        // Get Reference of the selected tool
        WorldForgerTool selectedTool = (WorldForgerTool)tools.GetArrayElementAtIndex(selectedToolIndex.intValue).objectReferenceValue;

        if (selectedTool != null)
        {
            if (selectedTool._terrain == null) selectedTool._terrain = _terrain;

            if (currentToolInstance != selectedTool)
            {
                if (currentToolInstance != null) currentToolInstance.ToolDeselected();

                currentToolInstance = selectedTool;
                //currentTool.objectReferenceValue = selectedTool;
                currentToolInstance.ToolSelected();
            }
        }

        EditorGUILayout.Separator();

        // Update the Inspector's Current Selected Tool
        if (currentToolInstance)
        {
            currentToolInstance.OnInspectorGUI();
        }

        serializedObject.ApplyModifiedProperties();


        foreach (var c in _terrain._chunks)
        {
            Debug.Log($"Chunk {c.Key} ->{c.Value._cells.Count}");

        }

    }

    public void OnSceneGUI()
    {
        if (_terrain == null) return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        _currentHandle = (EditorGUIUtility.hotControl != 0) ? EditorGUIUtility.hotControl : _currentHandle;
        Tools.current = Tool.None; // Turn off unity's tools (Move, rotate, etc...)

        // Update the selected tool and handle interactions
        if (currentToolInstance != null)
        {
            currentToolInstance.Update();
            currentToolInstance.DrawHandles();
            HandleEvents(currentToolInstance);
        }

        SceneView.RepaintAll();
    }

    void HandleEvents(WorldForgerTool tool)
    {
        Event current = Event.current;
        switch (current.type)
        {
            case EventType.MouseDown:
                tool.OnMouseDown(current.button);
                break;
            case EventType.MouseDrag:
                tool.OnMouseDrag(current.delta);
                break;
            case EventType.MouseUp:
                tool.OnMouseUp(current.button);
                break;
        }
    }
}