using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

/* Tool for terrain texturization, this tool interacts with the chunk's meshes and paints them with textures*/
[System.Serializable]
public class TextureTool : WorldForgerTool
{
    //public new string _toolName = "Texture Tool";

    [SerializeField]
    Texture2D[] layers;

    GUIContent[] textureContent;

    bool isFallOff;

    float brushSize = 2;

    int selectedTexture;

    //GUIStyle labelStyle;

    private AnimationCurve falloffCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.0f);

    public Color color = Color.white;

    public override void Start()
    {
        Debug.Log("Start!");
        _toolName = "Texture Tool";
    }

    public override void Update() 
    {
        selectedCells?.Clear();


        /* SCROLLING*/
        if (Event.current.type == EventType.ScrollWheel) 
        {
            brushSize += -Event.current.delta.y * 0.1f;
            brushSize = Mathf.Clamp(brushSize, 1, 100);

            Event.current.Use(); // Omit event to prevent zooming on the Scene Window.
        }


       // Sets the color acording to the selected texture
        switch (selectedTexture)
        {
            case 0:
                {
                    color = new Color(1, 0, 0, 0);
                }
                break;
            case 1:
                {
                    color = new Color(0, 1, 0, 0);
                }
                break;
            case 2:
                {
                    color = new Color(0, 0, 1, 0);
                }
                break;
            case 3:
                {
                    color = new Color(0, 0, 0, 1);
                }
                break;
        }


        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, _terrain.transform.position);
        groundPlane.Raycast(ray, out float distance);

        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, 1000, 1 << _terrain.gameObject.layer);
        mousePosition = hit ? new Vector3(hitInfo.point.x, 0, hitInfo.point.z) : ray.GetPoint(distance);

        // Selecting Cells
        for (int y = -Mathf.FloorToInt(brushSize / 2); y <= Mathf.FloorToInt(brushSize / 2); ++y)
        {
            for (int x = -Mathf.FloorToInt(brushSize / 2); x <= Mathf.FloorToInt(brushSize / 2); ++x)
            {
                Vector3 point = new Vector3(x, 0, y);
                Vector3 mouseOffset = mousePosition + point;
                Vector3 cellWorld = Snap(mouseOffset, _terrain._settings.cellSize.x, 1, _terrain._settings.cellSize.y);

                bool insideRadius = Vector3.Distance(Snap(mousePosition, _terrain._settings.cellSize.x, 1, _terrain._settings.cellSize.y), cellWorld) < brushSize / 2;

                List<Chunk> chunks = _terrain.GetChunksAtWorldPosition(cellWorld);

                //Get chunk position at mouseOffset
                Vector2Int chunk = new Vector2Int(
                    Mathf.FloorToInt(mouseOffset.x / totalTerrainSize.x),
                    Mathf.FloorToInt(mouseOffset.z / totalTerrainSize.z)
                );

                if (!selectedCells.ContainsKey(cellWorld) && insideRadius && chunks.Count > 0)
                {
                    selectedCells.Add(cellWorld, chunk);
                }
            }
        }

        /*Painting*/
        if (isMouseDown)
        {
            _terrain.DrawColors(selectedCells, mousePosition, brushSize, color, isFallOff);
        }

        /* Falloff switch*/
        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.LeftShift)
            {
                isFallOff = true;
            }
        }
        else if (Event.current.type == EventType.KeyUp)
        {
            if (Event.current.keyCode == KeyCode.LeftShift)
            {
                isFallOff = false;
            }
        }
    }

    public override void OnMouseDown(int button = 0) 
    {
        if (button == 0)
        {
            isMouseDown = true;
        }
    }

    public override void OnMouseUp(int button = 0) 
    {
        if (button == 0)
        {
            isMouseDown = false;
        }
    }

    public override void OnMouseDrag(Vector2 delta) 
    { 
    
    }

    public override void DrawHandles() 
    {
        /*
        Handles.color = Color.green;

        foreach (var cell in selectedCells)
        {
            if (!_terrain._chunks.ContainsKey(cell.Value))
            {
                selectedCells.Clear();
                break;
            }
            Chunk c = _terrain._chunks[cell.Value];
            Vector2Int localCellPos = new Vector2Int(
                Mathf.FloorToInt((cell.Key.x - c.transform.position.x) / _terrain._settings.cellSize.x),
                Mathf.FloorToInt((cell.Key.z - c.transform.position.z) / _terrain._settings.cellSize.y)
            );

            Handles.color = new Color(0, 1, 0, .5f);
            float cellHeight = c.GetHeightMapByIndex(c.GetIndex(localCellPos.y, localCellPos.x));
            Handles.DrawSolidDisc(cell.Key + Vector3.up * cellHeight, Vector3.up, _terrain._settings.cellSize.x / 2);

            Handles.color = Color.yellow;
            Vector3 chunkWorldPos = new Vector3(
                c.transform.position.x + totalTerrainSize.x / 2,
                0,
                c.transform.position.z + totalTerrainSize.z / 2
            );
            Handles.DrawWireCube(chunkWorldPos, totalTerrainSize);

        }

        selectedCells.Clear();
        */

        falloffCurve = AnimationCurve.Linear(0, 1, 1, 0);
        //labelStyle.normal.textColor = Color.black;
        Handles.color = Color.green;
        foreach (var cell in selectedCells)
        {
            var marchingSquaresChunks = _terrain.GetChunksAtWorldPosition(cell.Key);

            if (marchingSquaresChunks.Count == 0)
                break;

            var dist = Vector3.Distance(cell.Key, Snap(mousePosition, _terrain._settings.cellSize.x, 1, _terrain._settings.cellSize.y));
            var falloff = isFallOff
                            ? falloffCurve.Evaluate(((brushSize / 2) - dist) / (brushSize / 2))
                            : 0;

            var c = marchingSquaresChunks[0];
            Vector2Int localCellPos = new Vector2Int(
                Mathf.FloorToInt((cell.Key.x - c.transform.position.x) / _terrain._settings.cellSize.x),
                Mathf.FloorToInt((cell.Key.z - c.transform.position.z) / _terrain._settings.cellSize.y)
            );

            float cellHeight = c.GetHeightMapByIndex(c.GetIndex(localCellPos.y, localCellPos.x));
            Handles.DrawSolidDisc(cell.Key + Vector3.up * cellHeight, Vector3.up, Mathf.Lerp(_terrain._settings.cellSize.x / 2, 0, falloff));
        }

        selectedCells.Clear();
    }

    public override void OnInspectorGUI() 
    {
        EditorGUILayout.LabelField("Brush Size: " + brushSize);
        EditorGUILayout.LabelField("Hold shift to enable falloff");

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        //Set width of horizontal layout 
        for (int i = 0; i < layers.Length; i++)
        {
            EditorGUILayout.BeginVertical();
            Texture2D tex = (Texture2D)EditorGUILayout.ObjectField("", layers[i], typeof(Texture2D), false, GUILayout.MaxWidth(64));
            if (GUILayout.Toggle(selectedTexture == i, textureContent[i]))
                selectedTexture = i;

            EditorGUILayout.EndVertical();
            if (tex != layers[i])
            {
                layers[i] = tex;
                UpdateMaterialLayers(layers);
            }
        }
        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();
    }

    public override void ToolSelected()
    {
        if (layers == null || layers.Length < 4)
        {
            layers = new Texture2D[4];
        }
        textureContent = new GUIContent[4]
        {
            new GUIContent("R"),
            new GUIContent("G"),
            new GUIContent("B"),
            new GUIContent("A")
        };
    }

    public override void ToolDeselected()
    {
        //Loop over all materials on chunks and set the keyword _OVERLAYVERTEXCOLORS to false
        //foreach (var chunk in t.chunks.Values)
        //{
        //    foreach (var mat in chunk.GetComponent<MeshRenderer>().sharedMaterials)
        //    {
        //        mat.DisableKeyword("_OVERLAYVERTEXCOLORS");
        //    }
        //}
    }

    void UpdateMaterialLayers(Texture2D[] l)
    {
        foreach (Chunk chunk in _terrain._chunks.Values)
        {
            foreach (Material mat in chunk.GetComponent<MeshRenderer>().sharedMaterials)
            {
                mat.SetTexture("_Ground1", layers[0]);
                mat.SetTexture("_Ground2", layers[1]);
                mat.SetTexture("_Ground3", layers[2]);
                mat.SetTexture("_Ground4", layers[3]);
            }
        }

    }
}
