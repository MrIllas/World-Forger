using UnityEditor;
using UnityEngine;


/* Tool for chunk creation and deletion. This tool takes care of properly creating or removing chunks from the terrain. */

[System.Serializable]
public class ChunkTool : WorldForgerTool
{

    private Vector2Int chunkPosition;
    private bool canPlace = false;

    public override void Start()
    {
        _toolName = "Chunk Tool";
    }

    public override void Update()
    {
        totalTerrainSize = new Vector3(
           (_terrain._settings.chunkSize - 1) * _terrain._settings.cellSize.x,
           0,
           (_terrain._settings.chunkSize - 1) * _terrain._settings.cellSize.y
       );


        //Raycast to terrain
        canPlace = false;
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, _terrain.transform.position);
        groundPlane.Raycast(ray, out float distance);
        mousePosition = ray.GetPoint(distance);

        chunkPosition = new Vector2Int(
            Mathf.FloorToInt(mousePosition.x / totalTerrainSize.x),
            Mathf.FloorToInt(mousePosition.z / totalTerrainSize.z)
        );

        //Convert chunk position to world position
        snappedMousePosition = new Vector3(
            (chunkPosition.x * (_terrain._settings.chunkSize - 1) * _terrain._settings.cellSize.x) + totalTerrainSize.x / 2,
            0,
            (chunkPosition.y * (_terrain._settings.chunkSize - 1) * _terrain._settings.cellSize.y) + totalTerrainSize.z / 2
        );

        if (_terrain._chunks.Count == 0)
        {
            canPlace = true;
        }
        else
        {

            if (_terrain._chunks.ContainsKey(chunkPosition))
            {
                canPlace = false;
                return;
            }
            Vector2Int[] neighborPositions = new Vector2Int[]
            {
                new Vector2Int(chunkPosition.x - 1, chunkPosition.y),
                new Vector2Int(chunkPosition.x + 1, chunkPosition.y),
                new Vector2Int(chunkPosition.x, chunkPosition.y - 1),
                new Vector2Int(chunkPosition.x, chunkPosition.y + 1)
            };

            bool hasNeighbors = false;
            foreach (Vector2Int pos in neighborPositions)
            {
                if (_terrain._chunks.ContainsKey(pos))
                {
                    hasNeighbors = true;
                    break;
                }
            }
            canPlace = hasNeighbors;
        }
    }

    public override void OnMouseDown (int button = 0)
    {
        if (button != 0)
            return;

        if (canPlace)
        {
            _terrain.AddNewChunk(chunkPosition.x, chunkPosition.y);
        }
        else
        {
            //if (t._chunks.ContainsKey(chunkPosition))
            _terrain.RemoveChunk(chunkPosition);
        }
    }

    public override void OnMouseUp(int button = 0)
    {

    }

    public override void OnMouseDrag(Vector2 delta)
    {

    }

    public override void DrawHandles()
    {
        Handles.color = canPlace ? Color.green : Color.red;
        Handles.DrawWireCube(snappedMousePosition, totalTerrainSize);
    }
}
