using System.Collections.Generic;
using UnityEngine;


/* Father of all terrain tools*/

[System.Serializable]
public class WorldForgerTool : ScriptableObject
{
    protected enum BrushToolState
    {
        None = 0,
        SelectingCells,
        SelectedCells,
        DraggingHeight
    }

    public string _toolName = "Empty";

    protected Vector3 totalTerrainSize;

    protected Vector3 mousePosition;
    protected Vector3 snappedMousePosition;
    protected Vector2 viewportMousePosition;

    protected bool isMouseDown;

    public WorldForger _terrain;

    //cell world position, chunkPos
    protected Dictionary <Vector3, Vector2Int> selectedCells = new Dictionary<Vector3, Vector2Int>();

    public virtual void Start() { }

    public virtual void Update() { }

    public virtual void OnMouseDown(int button = 0) { }
    public virtual void OnMouseUp(int button = 0) { }
    public virtual void OnMouseDrag(Vector2 delta) { }

    public virtual void DrawHandles() { }
    public virtual void OnInspectorGUI() { }

    public virtual void ToolSelected() 
    {
        //Debug.Log("Tool '"+_toolName+"' selected");
    }
    public virtual void ToolDeselected() 
    {
        //Debug.Log("Tool '" + _toolName + "' deselected");
    }

    protected Vector3 Snap(Vector3 v, float snapX, float snapY, float snapZ)
    {
        return new Vector3(
            Mathf.Round(v.x / snapX) * snapX,
            Mathf.Round(v.y / snapY) * snapY,
            Mathf.Round(v.z / snapZ) * snapZ
        );
    }
}
