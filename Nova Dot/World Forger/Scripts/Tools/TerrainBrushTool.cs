using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/* Tool for terrain deformation. This tool edits the vertices of the chunk's meshes*/
[System.Serializable]
public class TerrainBrushTool : WorldForgerTool
{
    Vector3 cellWorldPosition;
    Vector2Int cellPosition;
    Vector2Int chunkPosition;

    BrushToolState state = BrushToolState.None;

    float dragHeight = 0;
    float snappedDragHeight = 0;

    float hoveredCellHeight = 0;

    float brushSize = 1;



    // Flags
    bool flattenGeometry = false;
    bool snapHeight = true;

    // Options values
    float snapValue = 0.5f;

    // Option labels
    GUIContent flattenLabel = new GUIContent("Flatten Geometry", "Averages the height of manipulated geometry so it'll all be at the same height");
    GUIContent snapHeightLabel = new GUIContent("Snap Height", "Snaps the height");

    public override void Update()
    {
        totalTerrainSize = new Vector3((_terrain._settings.chunkSize - 1) * _terrain._settings.cellSize.x, 0,(_terrain._settings.chunkSize - 1) * _terrain._settings.cellSize.y);

        if (state == BrushToolState.None && selectedCells.Count > 0)
        {
            selectedCells.Clear();
        }

        viewportMousePosition = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
        
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        //Plane groundPlane = new Plane(Vector3.up, _terrain.transform.position);

        //float distance = 0;
        //groundPlane.Raycast(ray, out float distance);
        float distance = 200.0f;
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, distance, 1 << _terrain.gameObject.layer);
        //bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, distance, _terrain.gameObject.layer);
        
        mousePosition = hit ? new Vector3(hitInfo.point.x, 0, hitInfo.point.z) : ray.GetPoint(distance);

        chunkPosition = new Vector2Int(
            Mathf.FloorToInt(mousePosition.x / totalTerrainSize.x),
            Mathf.FloorToInt(mousePosition.z / totalTerrainSize.z)
        );

        if (Event.current.type == EventType.ScrollWheel)
        {
            brushSize += -Event.current.delta.y * 0.1f;
            brushSize = Mathf.Clamp(brushSize, 1, 100);
            
            //Blocks scene zoom
            Event.current.Use();
        }

        // Selecting Cells
        if (state == BrushToolState.SelectingCells)
        {
            for (int y = -Mathf.FloorToInt(brushSize / 2); y <= Mathf.FloorToInt(brushSize / 2); y++)
            {
                for (int x = -Mathf.FloorToInt(brushSize / 2); x <= Mathf.FloorToInt(brushSize / 2); x++)
                {
                    Vector3 p = new Vector3(x, 0, y);
                    Vector3 mouseOffset = mousePosition + p;
                    
                    //Snap to cell size
                    Vector3 cellWorld = Snap(mouseOffset, _terrain._settings.cellSize.x, 1, _terrain._settings.cellSize.y);

                    //Get chunk position at mouseOffset
                    Vector2Int chunk = new Vector2Int(
                        Mathf.FloorToInt(mouseOffset.x / totalTerrainSize.x),
                        Mathf.FloorToInt(mouseOffset.z / totalTerrainSize.z)
                    );

                    //float wX = (chunk.x * (_terrain._settings.chunkSize - 1)) + x;
                    //float wZ = (chunk.y * (_terrain._settings.chunkSize - 1)) + y;

                    bool insideRadius = Vector3.Distance(Snap(mousePosition, _terrain._settings.cellSize.x, 1, _terrain._settings.cellSize.y), mouseOffset) <= brushSize / 2;

                    if (_terrain._chunks.ContainsKey(chunk) && !selectedCells.ContainsKey(cellWorld) && insideRadius)
                    {
                        selectedCells[cellWorld] = chunk;
                    }
                }
            }
        }

        cellWorldPosition = Snap(mousePosition, _terrain._settings.cellSize.x, 1, _terrain._settings.cellSize.y);

        if (_terrain._chunks.ContainsKey(chunkPosition))
        {
            cellPosition = new Vector2Int(
                Mathf.FloorToInt((cellWorldPosition.x - _terrain._chunks[chunkPosition].transform.position.x) / _terrain._settings.cellSize.x),
                Mathf.FloorToInt((cellWorldPosition.z - _terrain._chunks[chunkPosition].transform.position.z) / _terrain._settings.cellSize.y)
            );
            
            if (state == BrushToolState.SelectingCells || state == BrushToolState.None)
            {
                //int index = ;
                //float aux = t._chunks[chunkPosition].GetHeightMapByIndex(index);
                hoveredCellHeight = _terrain._chunks[chunkPosition].GetHeightMapByIndex(_terrain._chunks[chunkPosition].GetIndex(cellPosition.y, cellPosition.x));
                //hoveredCellHeight = _terrain._chunks[chunkPosition]._meshData.heightMap[_terrain._chunks[chunkPosition].GetIndex(cellPosition.y, cellPosition.x)];
            }
        }
    }

    public override void OnMouseDown(int button = 0)
    {
        if (button == 0)
        {
            switch (state)
            {
                case BrushToolState.None:
                    {
                        state = BrushToolState.SelectingCells;
                        break;
                    }
                case BrushToolState.SelectedCells:
                    {
                        state = BrushToolState.DraggingHeight;
                        break;
                    }
            }
        }
    }

    public override void OnMouseUp(int button = 0)
    {
        if (button != 0)
            return;

        switch (state)
        {
            case BrushToolState.SelectingCells:
                {
                    if (selectedCells.Count > 0)
                        state = BrushToolState.SelectedCells;
                    else
                        state = BrushToolState.None;
                    break;
                } 
            case BrushToolState.DraggingHeight: // Generates The New Terrain Modification
                {
                    // TODO : BUG WHEN RISING TERRAIN BY 0.5F, THE VERTEX GET DELETED

                    //Remove any selectedCells with the same world position
                    float avgHeight = 0;

                    var finalDragHeight = (snapHeight ? snappedDragHeight : dragHeight);

                    foreach (var cell in selectedCells)
                    {
                        Chunk c = _terrain._chunks[cell.Value];
                        Vector2Int localCellPos = new Vector2Int(
                            Mathf.FloorToInt((cell.Key.x - c.transform.position.x) / _terrain._settings.cellSize.x),
                            Mathf.FloorToInt((cell.Key.z - c.transform.position.z) / _terrain._settings.cellSize.y)
                        );

                        float curHeight = c.GetHeightMapByIndex(c.GetIndex(localCellPos.y, localCellPos.x));
                        if (flattenGeometry)
                        {
                            avgHeight += curHeight;
                        }

                        if (!flattenGeometry)
                        {
                            _terrain.SetHeight(cell.Value, localCellPos.x, localCellPos.y, curHeight + finalDragHeight);
                        }
                    }

                    if (flattenGeometry)
                    {
                        avgHeight /= selectedCells.Count;
                        foreach (var cell in selectedCells)
                        {
                            Chunk c = _terrain._chunks[cell.Value];
                            //Convert world position to chunk cell position
                            Vector2Int localCellPos = new Vector2Int(
                                Mathf.FloorToInt((cell.Key.x - c.transform.position.x) / _terrain._settings.cellSize.x),
                                Mathf.FloorToInt((cell.Key.z - c.transform.position.z) / _terrain._settings.cellSize.y)
                            );
                            _terrain.SetHeight(cell.Value, localCellPos.x, localCellPos.y, avgHeight + finalDragHeight);
                        }
                    }

                    dragHeight = 0;
                    snappedDragHeight = 0;
                    state = BrushToolState.None;

                    break;
                }
        }

        isMouseDown = false;
    }

    public override void OnMouseDrag(Vector2 delta)
    {
        switch (state)
        {
            case BrushToolState.DraggingHeight:
                {
                    dragHeight += -delta.y * 0.1f;
                    snappedDragHeight = Mathf.Round(dragHeight / snapValue) * snapValue;
                    break;
                }
        }
    }

    public override void DrawHandles()
    {
        Handles.color = new Color(0.8f, 0.8f, 0.8f, 0.4f);

        if (state != BrushToolState.DraggingHeight)
        {
            Handles.DrawSolidDisc(cellWorldPosition + Vector3.up * hoveredCellHeight, Vector3.up, brushSize / 2);
        }

        // Initialize min and max to track the bounds of all the selected squares
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;

        var finalDragHeight = (snapHeight ? snappedDragHeight : dragHeight);

        // Loop through selected cells and draw their squares
        foreach (var cell in selectedCells)
        {
            if (!_terrain._chunks.ContainsKey(cell.Value))
            {
                selectedCells.Clear();
                state = BrushToolState.None;
                break;
            }

            Chunk c = _terrain._chunks[cell.Value];
            Vector2Int localCellPos = new Vector2Int(
                Mathf.FloorToInt((cell.Key.x - c.transform.position.x) / _terrain._settings.cellSize.x),
                Mathf.FloorToInt((cell.Key.z - c.transform.position.z) / _terrain._settings.cellSize.y)
            );

            float cellHeight = c.GetHeightMapByIndex(c.GetIndex(localCellPos.y, localCellPos.x));
            Vector3 center = cell.Key + Vector3.up * (cellHeight + finalDragHeight);
            float size = _terrain._settings.cellSize.x * 0.5f;

            Vector3[] squareCorners = new Vector3[]
            {
            center + new Vector3(-size / 2, 0, -size / 2), // Bottom Left
            center + new Vector3(size / 2, 0, -size / 2),  // Bottom Right
            center + new Vector3(size / 2, 0, size / 2),   // Top Right
            center + new Vector3(-size / 2, 0, size / 2)   // Top Left
            };

            // Draw the solid square with custom color
            Handles.color = Color.white;
            Color squareColor = new Color(0.8f, 0.8f, 0.8f, 0.2f);
            Handles.DrawSolidRectangleWithOutline(squareCorners, squareColor, squareColor);

            // Update the overall bounds (min and max) to include the current square
            foreach (var corner in squareCorners)
            {
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
            }
        }

        // After processing all selected cells, draw a large yellow wireframe square around all of them
        if (selectedCells.Count > 0)
        {
            Handles.color = Color.yellow; // Set the color to yellow
            Vector3 centerOfBounds = (min + max) / 2;
            float margin = 0.5f;
            Vector3 sizeOfBounds = new Vector3(max.x - min.x + margin, 0, max.z - min.z + margin);

            // Draw a wireframe around the entire area covered by all squares
            Handles.DrawWireCube(centerOfBounds, sizeOfBounds);
        }

        // Draw wire around hovered chunk
        if (_terrain._chunks.ContainsKey(chunkPosition))
        {
            Handles.color = Color.yellow;
            Vector3 chunkWorldPos = new Vector3(
                _terrain._chunks[chunkPosition].transform.position.x + totalTerrainSize.x / 2,
                0,
                _terrain._chunks[chunkPosition].transform.position.z + totalTerrainSize.z / 2
            );
            Handles.DrawWireCube(chunkWorldPos, totalTerrainSize);
        }
    }


    
    public override void OnInspectorGUI()
    {
        flattenGeometry = EditorGUILayout.Toggle(flattenLabel, flattenGeometry);
        snapHeight = EditorGUILayout.Toggle(snapHeightLabel, snapHeight);

        snapValue = EditorGUILayout.Slider(snapValue, 0.1f, 1.0f);
    }
}
