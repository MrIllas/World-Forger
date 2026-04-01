using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

/* This class is the brain of the terrain system, it manages chunks, and the terrain as a whole. Like the Terrain Component in unity*/

public class WorldForger : MonoBehaviour
{
    public WorldForgerSettings _settings;

    // Keys (Chunk Position Ex: (0,0) or (-1, 2)), Value (Chunk)
    public WF_SerializedDictionary<Vector2Int, Chunk> _chunks = new WF_SerializedDictionary<Vector2Int, Chunk>();

    public float slopeThreshold = 0.6f;

    // Tools
    public int _selectedTool = 0;
    public WorldForgerTool[] _tools;
    public WorldForgerTool _currentTool;
    public WorldForgerTool _lastTool;

    private void OnValidate()
    {
        if (_settings == null)
        {
            _settings = WorldForgerSettings.Instance;
        }
    }


    public void AddNewChunk(int chunkX, int chunkY)
    {
        var chunkCoords = new Vector2Int(chunkX, chunkY);
        var newChunk = new GameObject("Chunk " + chunkCoords);

        Chunk chunk = newChunk.AddComponent<Chunk>();
        newChunk.AddComponent<MeshFilter>();
        MeshRenderer mr = newChunk.AddComponent<MeshRenderer>();
        newChunk.AddComponent<MeshCollider>();
        newChunk.layer = gameObject.layer;

        mr.shadowCastingMode = ShadowCastingMode.TwoSided;
        newChunk.isStatic = true;

        AddChunk(chunkCoords, chunk);

        var chunkLeft = _chunks.TryGetValue(new Vector2Int(chunkX - 1, chunkY), out var leftChunk);
        if (chunkLeft)
        {
            for (int z = 0; z < _settings.chunkSize; z++)
            {
                //int idx1 = chunk.GetIndex(z, 0);
                int idx2 = leftChunk.GetIndex(z, _settings.chunkSize - 1);
                chunk.DrawHeight(0, z, leftChunk.GetHeightMapByIndex(idx2));
            }
        }

        var chunkRight = _chunks.TryGetValue(new Vector2Int(chunkX + 1, chunkY), out var rightChunk);
        if (chunkRight)
        {
            for (int z = 0; z < _settings.chunkSize; z++)
            {
                //int idx1 = chunk.GetIndex(z, _settings.chunkSize - 1);
                int idx2 = rightChunk.GetIndex(z, 0);
                chunk.DrawHeight(_settings.chunkSize - 1, z, rightChunk.GetHeightMapByIndex(idx2));
            }
        }

        var chunkUp = _chunks.TryGetValue(new Vector2Int(chunkX, chunkY + 1), out var upChunk);
        if (chunkUp)
        {
            for (int x = 0; x < _settings.chunkSize; x++)
            {
                //int idx1 = chunk.GetIndex(_settings.chunkSize - 1, x);
                int idx2 = upChunk.GetIndex(0, x);
                chunk.DrawHeight(x, _settings.chunkSize - 1, upChunk.GetHeightMapByIndex(idx2));

            }
        }

        var chunkDown = _chunks.TryGetValue(new Vector2Int(chunkX, chunkY - 1), out var downChunk);
        if (chunkDown)
        {
            for (int x = 0; x < _settings.chunkSize; x++)
            {
                //int idx1 = chunk.GetIndex(0, x);
                int idx2 = downChunk.GetIndex(_settings.chunkSize - 1, x);
                chunk.DrawHeight(x, 0, downChunk.GetHeightMapByIndex(idx2));
            }
        }

        chunk.GenerateMesh();
    }

    void AddChunk(Vector2Int coords, Chunk chunk, bool regenMesh = true)
    {
        // First remove the chunk if the chunk that we are trying to create already exists
        RemoveChunk(coords);

        _chunks.Add(coords, chunk);
        chunk.terrain = this;
        chunk.chunkPosition = coords;

        chunk.transform.position = new Vector3(
            (coords.x * ((_settings.chunkSize - 1) * _settings.cellSize.x)),
            0,
            (coords.y * ((_settings.chunkSize - 1) * _settings.cellSize.y))
        );

        chunk.transform.parent = transform;
        chunk.InitializeMesh();
    }

    public void RemoveChunk(Vector2Int coords)
    {
        if (_chunks.ContainsKey(coords))
        {
            if (_chunks[coords] != null) DestroyImmediate(_chunks[coords].gameObject);

            _chunks.Remove(coords);
        }
    }

    public void SetHeight(Vector2Int chunk, float cx, float cz, float height) 
    {
        Chunk c = _chunks[chunk];
        c.DrawHeight((int) cx, (int) cz, height);

        //Chunk to the left, setting cell on the left edge
        var chunkLeft = _chunks.TryGetValue(new Vector2Int(chunk.x - 1, chunk.y), out var leftChunk) && cx == 0;
        if (chunkLeft)
        {
            if (leftChunk.GetHeightMapByIndex(leftChunk.GetIndex((int)cz, _settings.chunkSize - 1)) != height)
            {
                SetHeight(leftChunk.chunkPosition, _settings.chunkSize - 1, cz, height);
            }
        }

        var chunkRight = _chunks.TryGetValue(new Vector2Int(chunk.x + 1, chunk.y), out var rightChunk) && cx == _settings.chunkSize - 1;
        if (chunkRight)
        {
            if (rightChunk.GetHeightMapByIndex(rightChunk.GetIndex((int)cz, 0)) != height)
            {
                SetHeight(rightChunk.chunkPosition, 0, cz, height);
            }
        }

        var chunkUp = _chunks.TryGetValue(new Vector2Int(chunk.x, chunk.y + 1), out var upChunk) && cz == _settings.chunkSize - 1;
        if (chunkUp)
        {
            if (upChunk.GetHeightMapByIndex(upChunk.GetIndex(0, (int)cx)) != height)
            {
                SetHeight(upChunk.chunkPosition, cx, 0, height);
            }
        }


        var chunkDown = _chunks.TryGetValue(new Vector2Int(chunk.x, chunk.y - 1), out var downChunk) && cz == 0;
        if (chunkDown)
        {
            if (downChunk.GetHeightMapByIndex(downChunk.GetIndex(_settings.chunkSize - 1, (int)cx)) != height)
            {
                SetHeight(downChunk.chunkPosition, cx, _settings.chunkSize - 1, height);
            }
        }
    }


    internal void DrawColors(Dictionary<Vector3, Vector2Int> worldCellPosition, Vector3 paintPos, float brushSize, Color color, bool isFallOff)
    {
        AnimationCurve fallOffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        foreach (var worldCell in worldCellPosition)
        {
            List<Chunk> chunksAtWorldPosition = GetChunksAtWorldPosition(worldCell.Key);

            foreach(Chunk chunk in chunksAtWorldPosition)
            {
                Vector2Int localPos = new Vector2Int(
                    Mathf.FloorToInt((worldCell.Key.x - chunk.transform.position.x) / _settings.cellSize.x),
                    Mathf.FloorToInt((worldCell.Key.z - chunk.transform.position.z) / _settings.cellSize.y)
                );

                float dist = Vector3.Distance(worldCell.Key, paintPos);
                float t = (brushSize / 2 - dist) / brushSize / 2;
                Color c = GetColor(worldCell.Key);

                chunk.DrawColor(localPos.x, localPos.y, isFallOff ? Color.Lerp(color, c, fallOffCurve.Evaluate(t)) : color);
            }
        }
    }

    /// <summary>
    /// Get the chunks at a given cell's world position
    /// </summary>
    /// <param name="worldCellPosition"></param>
    internal List<Chunk> GetChunksAtWorldPosition(Vector3 worldCellPosition)
    {
        var chunksAtPosition = new List<Chunk>();
        //Loop through every chunk and check if the world position is inside the chunk
        foreach (var chunk in _chunks)
        {
            var localCellPos = new Vector2Int(
                Mathf.FloorToInt((worldCellPosition.x - chunk.Value.transform.position.x) / _settings.cellSize.x),
                Mathf.FloorToInt((worldCellPosition.z - chunk.Value.transform.position.z) / _settings.cellSize.y)
            );
            var inBounds = !(localCellPos.x < 0 || localCellPos.x >= _settings.chunkSize || localCellPos.y < 0 || localCellPos.y >= _settings.chunkSize);
            if (inBounds && !chunksAtPosition.Contains(chunk.Value))
            {
                chunksAtPosition.Add(chunk.Value);
            }
        }

        //chunksAtPosition = chunksAtPosition.Distinct().ToList();
        chunksAtPosition = chunksAtPosition.Distinct().ToList();

        return chunksAtPosition;
    }

    internal Color GetColor(Vector3 worldCellPos)
    {
        Color color = Color.white;
        List<Chunk> chunksAtWorldPosition = GetChunksAtWorldPosition(worldCellPos);

        foreach (Chunk chunk in chunksAtWorldPosition)
        {
            //Get the local cell position
            var localCellPos = new Vector2Int(
                Mathf.FloorToInt((worldCellPos.x - chunk.transform.position.x) / _settings.cellSize.x),
                Mathf.FloorToInt((worldCellPos.z - chunk.transform.position.z) / _settings.cellSize.y)
            );

            //float4 c = chunk._meshData.colorMap[chunk.GetIndex(localCellPos.x, localCellPos.y)];

            Color c = chunk._meshData.colorMap[chunk.GetIndex(localCellPos.x, localCellPos.y)];
            //color = new Color(c.x, c.y, c.z, c.w);
        }
        return color;
    }
}
