using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public struct MeshData
{
    public Mesh mesh;

    public List<Vector3> vertices;
    public List<Color> colors;
    //public List<float4> colors;
    public List<int> triangles;
    public List<Vector2> uvs;

    public float[] heightMap;
    public Color[] colorMap;
    //public float4[] colorMap;
    public bool[] update;


    public bool isDirty;
    //public void Clean()
    //{
    //    mesh.Clear();
    //    vertices = new List<Vector3>();
    //    colors = new List<Color>();
    //    triangles = new List<int>();
    //    uvs = new List<Vector2>();
    //}
}

[System.Serializable]
public struct CellData
{
    public List<Vector3> vertices;
    public List<Vector2> uvs;
    public List<Color> colors;
    //public List<float4> colors;
}

/* This class is used to store and manage each individual chunk of the terrain, when manipulation the terrain we are manipulating this game object.
 Basically a chunk === a space of the terrain*/

public class Chunk : MonoBehaviour
{
    public WorldForger terrain;

    public MeshData _meshData;
    public WF_SerializedDictionary<Vector2Int, CellData> _cells = new WF_SerializedDictionary<Vector2Int, CellData>();
    public Vector2Int chunkPosition;
    private Vector2Int cellCoords;
    private List<bool> cellEdges;
    private List<float> pointHeights;

    private int r;

    private float ay;
    private float by;
    private float cy;
    private float dy;

    private bool ab;
    private bool ac;
    private bool bd;
    private bool cd;

    public bool higherPolyFloors = true;
    private bool floorMode;

    public Mesh GetMesh() { return _meshData.mesh; }
    public float GetHeightMapByIndex(int index) { return _meshData.heightMap[index];}

    public void InitializeMesh()
    {
        _meshData.mesh = new Mesh();
        _cells = new WF_SerializedDictionary<Vector2Int, CellData>();

        _meshData.colorMap = new Color[terrain._settings.chunkSize * terrain._settings.chunkSize];
        //_meshData.colorMap = new float4[terrain._settings.chunkSize];


        //Fill with red
        for (int z = 0; z < terrain._settings.chunkSize; z++)
        {
            for (int x = 0; x < terrain._settings.chunkSize; x++)
            {
                _meshData.colorMap[GetIndex(x, z)] = new Color(1, 0, 0, 0);
                //_meshData.colorMap[GetIndex(x, z)] = new float4(1, 0, 0, 0);
            }
        }

        _meshData.heightMap = new float[terrain._settings.chunkSize * terrain._settings.chunkSize];
        _meshData.update = new bool[terrain._settings.chunkSize * terrain._settings.chunkSize];

        for (int z = 0; z < terrain._settings.chunkSize; ++z) 
        {
            for (int x = 0; x < terrain._settings.chunkSize; ++x)
            {
                _meshData.update[GetIndex(x, z)] = true;
            }
        }

        GenerateMesh();
    }

    public void DrawHeight(int x, int z, float y)
    {
        _meshData.heightMap[GetIndex(z, x)] = y;
        NotifyUpdate(z, x);
        NotifyUpdate(z, x - 1);
        NotifyUpdate(z - 1, x);
        NotifyUpdate(z - 1, x - 1);
        GenerateMesh();
    }

    ////Cast float4 to color
    //public static float4 ToFloat4(this Color c)
    //{
    //    return new float4(c.r, c.g, c.b, c.a);
    //}


    public void DrawColor (int x, int z, Color color)
    {
        if (!isInBounds(x, z)) return;

        //_meshData.colorMap[GetIndex(x, z)] = new float4(color.r, color.g, color.b, color.a);
        _meshData.colorMap[GetIndex(x, z)] = color;
        _meshData.isDirty = true;
    }

    internal void DrawColors(List<Vector2Int> value, Color color)
    {
        for (int i = 0; i < value.Count; ++i)
        {
            if (!isInBounds(value[i].x, value[i].y)) continue;

            //_meshData.colorMap[GetIndex((int)value[i].x, (int)value[i].y)] = new float4(color.r, color.g, color.b, color.a);
            _meshData.colorMap[GetIndex((int)value[i].x, (int)value[i].y)] = color;
        }
        _meshData.isDirty = true;
    }

    public void GenerateMesh()
    {
       // _meshData.Clean();

        _meshData.mesh.Clear();
        _meshData.vertices = new List<Vector3>();
        _meshData.colors = new List<Color>();
        //_meshData.colors = new List<float4>();
        _meshData.triangles = new List<int>();
        _meshData.uvs = new List<Vector2>();

        GenerateChunkCells();

        _meshData.mesh.vertices = _meshData.vertices.ToArray();
        _meshData.mesh.triangles = _meshData.triangles.ToArray();

        Color[] vertexColors = new Color[_meshData.mesh.vertices.Length];
        //float4[] vertexColors = new float4[_meshData.mesh.vertices.Length];
        for (int i = 0; i < _meshData.colors.Count; ++i)
        {
            vertexColors[i] = _meshData.colors[i];
            //vertexColors[i] = _meshData.colors[i];
        }
        _meshData.mesh.colors = vertexColors;
        //_meshData.mesh.colors = new Color[vertexColors., vertexColors.Y];

        _meshData.mesh.uv = _meshData.uvs.ToArray();
        _meshData.mesh.RecalculateNormals();

        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        MeshFilter filter = gameObject.GetComponent<MeshFilter>();

        renderer.material = terrain._settings.terrainMaterial;
        filter.sharedMesh = _meshData.mesh;

        MeshCollider collider = gameObject.GetComponent<MeshCollider>();
        collider.sharedMesh = filter.sharedMesh;
    }

    public void GenerateChunkCells()
    {
        if (_cells == null) _cells = new WF_SerializedDictionary<Vector2Int, CellData> ();

        for (int z = 0; z < terrain._settings.chunkSize - 1; ++z)
        {
            for (int x = 0; x < terrain._settings.chunkSize - 1; ++x) 
            {
                cellCoords = new Vector2Int(x, z);

                if (!_meshData.update[GetIndex(z, x)])
                {
                    List<Vector3> vs = _cells[cellCoords].vertices;
                    List<Vector2> uvs = _cells[cellCoords].uvs;
                    List<Color> cols = _cells[cellCoords].colors;

                    for (int i = 0; i < vs.Count; i += 3 )
                    {
                        AddFace(vs[i], vs[i + 1], vs[i + 2], false);
                    }

                    for (int i = 0; i < cols.Count; ++i)
                    {
                        _meshData.colors.Add(cols[i]);
                    }

                    for (int i = 0; i < uvs.Count; ++i)
                    {
                        _meshData.uvs.Add(uvs[i]);
                    }

                    continue;
                }

                _meshData.update[GetIndex(z, x)] = false;
                _cells[cellCoords] = new CellData()
                {
                    vertices = new List<Vector3>(),
                    uvs = new List<Vector2>(),
                    colors = new List<Color>()
                };

                r = 0;

                ay = _meshData.heightMap[GetIndex(z, x)];
                by = _meshData.heightMap[GetIndex(z, x + 1)];
                cy = _meshData.heightMap[GetIndex(z + 1, x)];
                dy = _meshData.heightMap[GetIndex(z + 1, x + 1)];

                ab = Mathf.Abs(ay - by) < terrain.slopeThreshold; // Top Edge
                ac = Mathf.Abs(ay - cy) < terrain.slopeThreshold; // Bottom Edge
                bd = Mathf.Abs(by - dy) < terrain.slopeThreshold; // Right Edge
                cd = Mathf.Abs(cy - dy) < terrain.slopeThreshold; // Left Edge

                //ab = Mathf.Abs(ay - by) <= terrain.slopeThreshold; // Top Edge
                //ac = Mathf.Abs(ay - cy) <= terrain.slopeThreshold; // Bottom Edge
                //bd = Mathf.Abs(by - dy) <= terrain.slopeThreshold; // Right Edge
                //cd = Mathf.Abs(cy - dy) <= terrain.slopeThreshold; // Left Edge

                // Case 0
                if (ab && ac && bd && cd)
                {
                    AddFullFloor();
                    continue;
                }

                cellEdges = new List<bool> { ab, bd, cd, ac };
                pointHeights = new List<float> { ay, by, dy, cy };

                bool caseFound = false;

                for (int i = 0;i < 4; ++i)
                {
                    r = i;

                    ab = cellEdges[r];
                    bd = cellEdges[(r + 1) % 4];
                    cd = cellEdges[(r + 2) % 4];
                    ac = cellEdges[(r + 3) % 4];

                    ay = pointHeights[r];
                    by = pointHeights[(r + 1) % 4];
                    dy = pointHeights[(r + 2) % 4];
                    cy = pointHeights[(r + 3) % 4];

                    caseFound = true;

                    // Case 1
                    if (isHigher(ay, by) && isHigher(ay, cy) && bd && cd)
                    {
                        AddOuterCorner(true, true);
                    } // Case 2
                    else if (isHigher(ay, cy) && isHigher(by, dy) && ab && cd)
                    {
                        AddEdge(true, true);
                    } // Case 3
                    else if (isHigher(ay, by) && isHigher(ay, cy) && isHigher(by, dy) && cd)
                    {
                        AddEdge(true, true, 0.5f, 1);
                        AddOuterCorner(false, true, true, by);
                    } // Case 4
                    else if (isHigher(by, ay) && isHigher(ay, cy) && isHigher(by, dy) && cd)
                    {
                        AddEdge(true, true, 0, 0.5f);
                        RotateCell(1);
                        AddOuterCorner(false, true, true, cy);
                    } // Case 5
                    else if (isLower(ay, by) && isLower(ay, cy) && isLower(dy, by) && isLower(dy, cy) && isMerged(by, cy))
                    {
                        AddInnerCorner(true, false);
                        AddDiagonalFloor(by, cy, true, true);
                        RotateCell(2);
                        AddInnerCorner(true, false);
                    } // Case 5.5
                    else if (isLower(ay, by) && isLower(ay, cy) && isLower(dy, by) && isLower(dy, cy) && isHigher(by, cy))
                    {
                        AddInnerCorner(true, false, true);
                        AddDiagonalFloor(cy, cy, true, true);

                        RotateCell(2);
                        AddInnerCorner(true, false, true);

                        RotateCell(-1);
                        AddOuterCorner(false, true);
                    } // Case 6
                    else if (isLower(ay, by) && isLower(ay, cy) && bd && cd)
                    {
                        AddInnerCorner(true, true);
                    } // Case 7
                    else if (isLower(ay, by) && isLower(ay, cy) && isHigher(dy, by) && isHigher(dy, cy) && isMerged(by, cy))
                    {
                        AddInnerCorner(true, false);
                        AddDiagonalFloor(by, cy, true, false);
                        RotateCell(2);
                        AddOuterCorner(false, true);
                    } // Case 8
                    else if (isLower(ay, by) && isLower(ay, cy) && isLower(dy, cy) && bd)
                    {
                        AddInnerCorner(true, false, true);

                        StartFloor();
                        AddFace(AddPoint(1, dy, 1), AddPoint(0.5f, dy, 1, 1, 0), AddPoint(1, (by + dy) / 2, 0.5f));
                        AddFace(AddPoint(1, by, 0), AddPoint(1, (by + dy) / 2, 0.5f), AddPoint(0.5f, by, 0, 0, 1));
                        AddFace(AddPoint(0.5f, by, 0, 0, 1), AddPoint(1, (by + dy) / 2, 0.5f), AddPoint(0, by, 0.5f, 1, 1));
                        AddFace(AddPoint(0.5f, dy, 1, 1, 0), AddPoint(0, by, 0.5f, 1, 1), AddPoint(1, (by + dy) / 2, 0.5f));
                        
                        StartWall();
                        AddFace(AddPoint(0, by, 0.5f), AddPoint(0.5f, dy, 1), AddPoint(0, cy, 0.5f));
                        AddFace(AddPoint(0.5f, cy, 1), AddPoint(0, cy, 0.5f), AddPoint(0.5f, dy, 1));
                        
                        StartFloor();
                        AddFace(AddPoint(0, cy, 1), AddPoint(0, cy, 0.5f, 0, 1), AddPoint(0.5f, cy, 1, 0, 1));
                    } // Case 9
                    else if (isLower(ay, by) && isLower(ay, cy) && isLower(dy, by) && cd)
                    {
                        AddInnerCorner(true, false, true);

                        StartFloor();
                        //D Corner
                        AddFace(AddPoint(1, dy, 1, 0, 0), AddPoint(0.5f, (dy + cy) / 2, 1, 0, 0), AddPoint(1, dy, 0.5f, 1, 0));
                        //C Corner
                        AddFace(AddPoint(0, cy, 1, 0, 0), AddPoint(0, cy, 0.5f, 0, 1), AddPoint(0.5f, (dy + cy) / 2, 1));

                        //Center floors
                        AddFace(AddPoint(0, cy, 0.5f, 0, 1), AddPoint(0.5f, cy, 0, 1, 1), AddPoint(0.5f, (dy + cy) / 2, 1, 0, 0));
                        AddFace(AddPoint(1, dy, 0.5f, 1, 0f), AddPoint(0.5f, (dy + cy) / 2, 1, 0, 0), AddPoint(0.5f, cy, 0, 1, 1));

                        //Walls to upper corner
                        StartWall();
                        AddFace(AddPoint(0.5f, cy, 0), AddPoint(0.5f, by, 0), AddPoint(1, dy, 0.5f));
                        AddFace(AddPoint(1, by, 0.5f), AddPoint(1, dy, 0.5f), AddPoint(0.5f, by, 0));

                        StartFloor();
                        AddFace(AddPoint(1, by, 0), AddPoint(1, by, 0.5f, 0, 1), AddPoint(0.5f, by, 0, 0, 1));
                    } // Case 10
                    else if (isLower(ay, by) && isLower(ay, cy) && isHigher(dy, cy) && bd)
                    {
                        AddInnerCorner(true, false, true, true, false);
                        RotateCell(1);
                        AddEdge(false, true);
                    } // Case 11
                    else if (isLower(ay, by) && isLower(ay, cy) && isHigher(dy, by) && cd)
                    {
                        AddInnerCorner(true, false, true, false, true);
                        RotateCell(2);
                        AddEdge(false, true);
                    } // Case 12
                    else if (isLower(ay, by) && isLower(by, dy) && isLower(dy, cy) && isHigher(cy, ay))
                    {
                        AddInnerCorner(true, false, true, false, true);
                        RotateCell(2);
                        AddEdge(false, true, 0, 0.5f);
                        RotateCell(1);
                        AddOuterCorner(false, true, true, cy);
                    } // Case 13
                    else if (isLower(ay, cy) && isLower(cy, dy) && isLower(dy, by) && isHigher(by, ay))
                    {
                        AddInnerCorner(true, false, true, true, false);
                        RotateCell(1);
                        AddEdge(false, true, 0.5f, 1);
                        AddOuterCorner(false, true, true, by);
                    } // Case 14
                    else if (isLower(ay, by) && isLower(by, cy) && isLower(cy, dy))
                    {
                        AddInnerCorner(true, false, true, false, true);
                        RotateCell(2);
                        AddEdge(false, true, 0.5f, 1);
                        AddOuterCorner(false, true, true, by);
                    } // Case 15
                    else if (isLower(ay, cy) && isLower(cy, by) && isLower(by, dy))
                    {
                        AddInnerCorner(true, false, true, true, false);
                        RotateCell(1);
                        AddEdge(false, true, 0, 0.5f);
                        RotateCell(1);
                        AddOuterCorner(false, true, true, cy);
                    } // Case 16
                    else if (ab && bd && cd && isHigher(ay, cy))
                    {
                        float edgeBy = (by + dy) / 2;
                        float edgeDy = (by + dy) / 2;

                        StartFloor();
                        AddFace(AddPoint(0, ay, 0), AddPoint(1, by, 0), AddPoint(1, edgeBy, 0.5f));
                        AddFace(AddPoint(1, edgeBy, 0.5f, 0, 1), AddPoint(0, ay, 0.5f, 0, 1), AddPoint(0, ay, 0));

                        StartWall();
                        AddFace(AddPoint(0, cy, 0.5f, 0, 0), AddPoint(0, ay, 0.5f, 0, 1), AddPoint(1, edgeDy, 0.5f, 1, 0));

                        StartFloor();
                        AddFace(AddPoint(0, cy, 0.5f, 1, 0), AddPoint(1, edgeDy, 0.5f, 1, 0), AddPoint(0, cy, 1));
                        AddFace(AddPoint(1, dy, 1), AddPoint(0, cy, 1), AddPoint(1, edgeDy, 0.5f));
                    } // Case 17
                    else if (ab && ac && cd && isHigher(by, dy))
                    {
                        var edgeAy = (ay + cy) / 2;
                        var edgeCy = (ay + cy) / 2;

                        StartFloor();
                        AddFace(AddPoint(0, ay, 0), AddPoint(1, by, 0), AddPoint(0, edgeAy, 0.5f));

                        AddFace(AddPoint(1, by, 0.5f, 0, 1), AddPoint(0, edgeAy, 0.5f, 0, 1), AddPoint(1, by, 0));

                        StartWall();
                        AddFace(AddPoint(1, by, 0.5f, 1, 1), AddPoint(1, dy, 0.5f, 1, 0), AddPoint(0, edgeAy, 0.5f, 0, 0));

                        StartFloor();
                        AddFace(AddPoint(0, edgeCy, 0.5f, 1, 0), AddPoint(1, dy, 0.5f, 1, 0), AddPoint(1, dy, 1));
                        AddFace(AddPoint(0, cy, 1), AddPoint(0, edgeCy, 0.5f), AddPoint(1, dy, 1));
                    } 
                    else
                    {
                        caseFound = false;
                    }

                    if (caseFound)
                    {
                        break;
                    }
                }
                
                if (!caseFound) 
                {
                    continue;
                }
            }
        }
    }

    #region Cell Help Methods
    
    void RotateCell(int rotations)
    {
        r = (r + 4 + rotations) % 4;
        
        ab = cellEdges[r];
        bd = cellEdges[(r + 1) % 4];
        cd = cellEdges[(r + 2) % 4];
        ac = cellEdges[(r + 3) % 4];

        ay = pointHeights[r];
        by = pointHeights[(r + 1) % 4];
        dy = pointHeights[(r + 2) % 4];
        cy = pointHeights[(r + 3) % 4];
    }

    void AddFullFloor()
    {
        StartFloor();

        if (higherPolyFloors)
        {
            var ey = (ay + by + cy + dy) / 4;
            AddFace(AddPoint(0, ay, 0), AddPoint(1, by, 0), AddPoint(0.5f, ey, 0.5f, 0, 0, true));
            AddFace(AddPoint(1, by, 0), AddPoint(1, dy, 1), AddPoint(0.5f, ey, 0.5f, 0, 0, true));
            AddFace(AddPoint(1, dy, 1), AddPoint(0, cy, 1), AddPoint(0.5f, ey, 0.5f, 0, 0, true));
            AddFace(AddPoint(0, cy, 1), AddPoint(0, ay, 0), AddPoint(0.5f, ey, 0.5f, 0, 0, true));
        }
        else
        {
            AddFace(AddPoint(0, ay, 0), AddPoint(1, by, 0), AddPoint(0, cy, 1));
            AddFace(AddPoint(1, dy, 1), AddPoint(0, cy, 1), AddPoint(1, by, 0));
        }
    }

    void AddOuterCorner(bool floorBelow = true, bool floorAbove = true, bool flattenBottom = false, float bottomHeight = -1)
    {
        float edgeBy = flattenBottom ? bottomHeight : by;
        float edgeCy = flattenBottom ? bottomHeight : cy;

        if (floorAbove)
        {
            StartFloor();
            AddFace(AddPoint(0, ay, 0, 0, 0), AddPoint(0.5f, ay, 0, 0, 1), AddPoint(0, ay, 0.5f, 0, 1));
        }

        StartWall();
        AddFace(AddPoint(0, edgeCy, 0.5f, 0, 0), AddPoint(0, ay, 0.5f, 0, 1), AddPoint(0.5f, edgeBy, 0, 1, 0));
        AddFace(AddPoint(0.5f, ay, 0, 1, 1), AddPoint(0.5f, edgeBy, 0, 1, 0), AddPoint(0, ay, 0.5f, 0, 1));

        if (floorBelow)
        {
            StartFloor();
            AddFace(AddPoint(1, dy, 1), AddPoint(0, cy, 1), AddPoint(1, by, 0));
            AddFace(AddPoint(0, cy, 1), AddPoint(0, cy, 0.5f, 1, 0), AddPoint(0.5f, by, 0, 1, 0));
            AddFace(AddPoint(1, by, 0), AddPoint(0, cy, 1), AddPoint(0.5f, by, 0, 1, 0));
        }
    }

    void AddInnerCorner(bool lowerFloor = true, bool fullUpperFloor = true, bool flatten = false, bool bdFloor = false, bool cdFloor = false)
    {
        var cornerBy = flatten ? Mathf.Min(by, cy) : by;
        var cornerCy = flatten ? Mathf.Min(by, cy) : cy;

        if (lowerFloor) 
        {
            StartFloor();
            AddFace(AddPoint(0, ay, 0), AddPoint(0.5f, ay, 0, 1, 0), AddPoint(0, ay, 0.5f, 1, 0));
        }

        StartWall();
        AddFace(AddPoint(0, ay, 0.5f, 1, 0), AddPoint(0.5f, ay, 0, 0, 0), AddPoint(0, cornerCy, 0.5f, 1, 1));
        AddFace(AddPoint(0.5f, cornerBy, 0, 0, 1), AddPoint(0, cornerCy, 0.5f, 1, 1), AddPoint(0.5f, ay, 0, 0, 0));

        StartFloor();
        if (fullUpperFloor)
        {
            AddFace(AddPoint(1, dy, 1), AddPoint(0, cornerCy, 1), AddPoint(1, cornerBy, 0));
            AddFace( AddPoint(0, cornerCy, 1), AddPoint(0, cornerCy, 0.5f, 0, 1), AddPoint(0.5f, cornerBy, 0, 0, 1));
            AddFace( AddPoint(1, cornerBy, 0), AddPoint(0, cornerCy, 1), AddPoint(0.5f, cornerBy, 0, 0, 1));
        }
        if (cdFloor)
        {
            AddFace(AddPoint(1, by, 0, 0, 0), AddPoint(0, by, 0.5f, 1, 1), AddPoint(0.5f, by, 0, 0, 1));

            AddFace(AddPoint(1, by, 0, 0, 0), AddPoint(1, by, 0.5f, 1, -1), AddPoint(0, by, 0.5f, 1, 1));
        }

        if (bdFloor)
        {
            AddFace(AddPoint(0, cy, 0.5f, 0, 1), AddPoint(0.5f, cy, 0, 1, 1), AddPoint(0, cy, 1, 0, 0));
            AddFace(AddPoint(0.5f, cy, 1, 1, -1), AddPoint(0, cy, 1, 0, 0), AddPoint(0.5f, cy, 0, 1, 1));
        }
    }

    void AddEdge(bool floorBelow, bool floorAbove, float aX = 0, float bX = 1) 
    {
        var edgeAy = ab ? ay : Mathf.Min(ay, by);
        var edgeBy = ab ? by : Mathf.Min(ay, by);
        var edgeCy = cd ? cy : Mathf.Max(cy, dy);
        var edgeDy = cd ? dy : Mathf.Max(cy, dy);

        if (floorAbove)
        {
            StartFloor();
            AddFace(
                AddPoint(aX, edgeAy, 0, aX < 0 ? 1 : 0, 0), 
                AddPoint(bX, edgeBy, 0, bX < 1 ? 1 : 0, 0), 
                AddPoint(0, edgeAy, 0.5f, bX < 1 ? -1 : (aX > 0 ? 1 : 0), 1)
            );
            AddFace(
                AddPoint(1, edgeBy, 0.5f, aX > 0 ? -1 : (bX < 1 ? 1 : 0), 1),
                AddPoint(0, edgeAy, 0.5f, bX < 1 ? -1 : (aX > 0 ? 1 : 0), 1),
                AddPoint(bX, edgeBy, 0, bX < 1 ? 1 : 0, 0)
            );
        }

        StartWall();
        AddFace(AddPoint(0, edgeCy, 0.5f, 0, 0), AddPoint(0, edgeAy, 0.5f, 0, 1), AddPoint(1, edgeDy, 0.5f, 1, 0));
        AddFace(AddPoint(1, edgeBy, 0.5f, 1, 1), AddPoint(1, edgeDy, 0.5f, 1, 0), AddPoint(0, edgeAy, 0.5f, 0, 1));
    
        if (floorBelow)
        {
            StartFloor();
            AddFace(AddPoint(0, cy, 0.5f, 1, 0), AddPoint(1, dy, 0.5f, 1, 0), AddPoint(0, cy, 1));
            AddFace(AddPoint(1, dy, 1), AddPoint(0, cy, 1), AddPoint(1, dy, 0.5f, 1, 0));
        }
    }

    void AddDiagonalFloor(float bY, float cY, bool aCliff, bool dCliff)
    {
        StartFloor();
        AddFace(AddPoint(1, bY, 0), AddPoint(0, cY, 1), AddPoint(0.5f, bY, 0, aCliff ? 0 : 1, aCliff ? 1 : 0));
        AddFace(
            AddPoint(0, cY, 1),
            AddPoint(0, cY, 0.5f, aCliff ? 0 : 1, aCliff ? 1 : 0),
            AddPoint( 0.5f, bY, 0, aCliff ? 0 : 1, aCliff ? 1 : 0)
         );

        AddFace(
            AddPoint(1, bY, 0), AddPoint( 1, bY, 0.5f, dCliff ? 0 : 1, dCliff ? 1 : 0), AddPoint(0, cY, 1));
        AddFace(
            AddPoint(0, cY, 1),
            AddPoint(1, bY, 0.5f, dCliff ? 0 : 1, dCliff ? 1 : 0),
            AddPoint( 0.5f, cY, 1, dCliff ? 0 : 1, dCliff ? 1 : 0)
        );
    }

    void AddFace(Vector3 v0, Vector3 v1, Vector3 v2, bool cache = true)
    {
        int meshVerticesCount = _meshData.vertices.Count;

        _meshData.vertices.Add(v0);
        _meshData.vertices.Add(v1);
        _meshData.vertices.Add(v2);

        _meshData.triangles.Add(meshVerticesCount + 2);
        _meshData.triangles.Add(meshVerticesCount + 1);
        _meshData.triangles.Add(meshVerticesCount);

        if (cache)
        {
            int cachedMeshVerticesCount = _cells[cellCoords].vertices.Count;
            _cells[cellCoords].vertices.Add(v0);
            _cells[cellCoords].vertices.Add(v1);
            _cells[cellCoords].vertices.Add(v2);
        }
    }

    Vector3 AddPoint(float x, float y, float z, float uvX = 0, float uvY = 0, bool diagMidpoint = false)
    {
        for (int i = 0; i < r; ++i)
        {
            var temp = x;
            x = 1 - z;
            z = temp;
        }

        var uv = floorMode ? new Vector2(uvX, uvY) : new Vector2(1, 1);

        Color color = Color.white;

        //float4 color = new float4(1,1,1,1);
        if (diagMidpoint)
        {
            int idx1 = GetIndex(cellCoords.x, cellCoords.y);
            int idx2 = GetIndex(cellCoords.x + 1, cellCoords.y);
            int idx3 = GetIndex(cellCoords.x, cellCoords.y + 1);
            int idx4 = GetIndex(cellCoords.x + 1, cellCoords.y + 1);

            var adColor = Color.Lerp(_meshData.colorMap[idx1], _meshData.colorMap[idx4], .5f);
            var bcColor = Color.Lerp(_meshData.colorMap[idx2], _meshData.colorMap[idx3], .5f);

            int cSize = terrain._settings.chunkSize;

            //var adColor = math.lerp(
            //    _meshData.colorMap[cellCoords.y * cSize + cellCoords.x],
            //    _meshData.colorMap[(cellCoords.y + 1) * cSize + cellCoords.x + 1],
            //    0.5f
            //);
            //var bcColor = math.lerp(
            //    _meshData.colorMap[cellCoords.y * cSize + cellCoords.x + 1],
            //    _meshData.colorMap[(cellCoords.y + 1) * cSize + cellCoords.x],
            //    0.5f
            //);

            color = new Color(
                Mathf.Min(adColor.r, bcColor.r),
                Mathf.Min(adColor.g, bcColor.g),
                Mathf.Min(adColor.b, bcColor.b),
                Mathf.Min(adColor.a, bcColor.a)
            );
            //color = new float4(
            //    Mathf.Min(adColor.x, bcColor.x),
            //    Mathf.Min(adColor.y, bcColor.y),
            //    Mathf.Min(adColor.z, bcColor.z),
            //    Mathf.Min(adColor.w, bcColor.w)
            //);

            if (adColor.r > 0.99 || bcColor.r > 0.99)
                color.r = 1;

            if (adColor.g > 0.99 || bcColor.g > 0.99)
                color.g = 1;

            if (adColor.b > 0.99 || bcColor.b > 0.99)
                color.b = 1;

            if (adColor.a > 0.99 || bcColor.a > 0.99)
                color.a = 1;

            //if (adColor.x > 0.99 || bcColor.x > 0.99)
            //    color.x = 1;

            //if (adColor.y > 0.99 || bcColor.y > 0.99)
            //    color.y = 1;

            //if (adColor.z > 0.99 || bcColor.z > 0.99)
            //    color.z = 1;

            //if (adColor.w > 0.99 || bcColor.w > 0.99)
            //    color.w = 1;
        }
        else
        {
            int idx = GetIndex(cellCoords.x, cellCoords.y);
            int idx2 = GetIndex(cellCoords.x + 1, cellCoords.y);
            int idx3 = GetIndex(cellCoords.x, cellCoords.y + 1);
            int idx4 = GetIndex(cellCoords.x + 1, cellCoords.y + 1);

            var abColor = Color.Lerp(_meshData.colorMap[idx], _meshData.colorMap[idx2], x);
            var cdColor = Color.Lerp(_meshData.colorMap[idx3], _meshData.colorMap[idx4], x);

            int cSize = terrain._settings.chunkSize;

            //var abColor = math.lerp(
            //     _meshData.colorMap[cellCoords.y * cSize + cellCoords.x],
            //     _meshData.colorMap[cellCoords.y * cSize + cellCoords.x + 1],
            //    x
            //);

            //var cdColor = math.lerp(
            //    _meshData.colorMap[(cellCoords.y + 1) * cSize + cellCoords.x],
            //    _meshData.colorMap[(cellCoords.y + 1) * cSize + cellCoords.x + 1],
            //    x
            //);

            color = Color.Lerp(abColor, cdColor, z);
            //color = math.lerp(abColor, cdColor, z);
        }

        Vector3 v = new Vector3(
            (cellCoords.x + x) * terrain._settings.cellSize.x,
            y,
            (cellCoords.y + z) * terrain._settings.cellSize.y
        );

        _meshData.colors.Add(color);
        _meshData.uvs.Add(uv);
        _cells[cellCoords].uvs.Add(uv);
        _cells[cellCoords].colors.Add(color);

        return v;
    }


    void StartFloor()
    {
        floorMode = true;
    }

    void StartWall()
    {
        floorMode = false;
    }

    #endregion

    #region Help Methods
    public int GetIndex(int x, int z)
    {
        //Check if within bounds
        if (x < 0 || x >= terrain._settings.chunkSize || z < 0 || z >= terrain._settings.chunkSize)
        {
            return 0;
        }

        return x + z * terrain._settings.chunkSize;
    }

    bool isHigher(float a, float b)
    {
        return a - b > terrain.slopeThreshold;
    }

    bool isLower(float a, float b)
    {
        return a - b < -terrain.slopeThreshold;
    }

    bool isMerged(float a, float b)
    {
        return Mathf.Abs(a - b) < terrain.slopeThreshold;
    }

    bool isInBounds(int x, int z)
    {
        return x >= 0 && x < terrain._settings.chunkSize && z >= 0 && z < terrain._settings.chunkSize;
    }

    void NotifyUpdate(int z, int x)
    {
        //Return if out of bounds
        if (x < 0 || x >= terrain._settings.chunkSize || z < 0 || z >= terrain._settings.chunkSize)
            return;

        _meshData.update[GetIndex(z, x)] = true;
    }

    #endregion
}
