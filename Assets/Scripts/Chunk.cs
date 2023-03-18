using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;
    
    public GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    private MeshCollider meshCollider;

    int vertexIndex;
    private List<Vector3> vertices = new();
    private List<int> triangles = new();
    private List<int> transparentTriangles = new ();
    private List<int> waterTriangles = new();
    private Material[] materials = new Material[3];
    List<Vector2> uvs = new();
    private List<Color> colors = new();
    private List<Vector3> normals = new List<Vector3>();

    public Vector3 position;
    
    private bool _isActive;

    public ChunkData chunkData;
    
    public List<VoxelState> activeVoxels = new List<VoxelState>();

    public void Reset()
    {
        chunkData?.Reset();
        chunkData = null;
    }
    public Chunk(ChunkCoord _coord)
    {
        coord = _coord;
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        
        materials[0] = World.Instance.material;
        materials[1] = World.Instance.transparentMaterial;
        materials[2] = World.Instance.waterMaterial;
        meshRenderer.materials = materials;
        
        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = $"Chunk {coord.x}, {coord.z}";
        position = chunkObject.transform.position;

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int((int)position.x, (int)position.z), true);
        chunkData.chunk = this;
        
        for (var y = 0; y < VoxelData.ChunkHeight; y++) {
            for (var x = 0; x < VoxelData.ChunkWidth; x++) {
                for (var z = 0; z < VoxelData.ChunkWidth; z++) {
                    var voxel = chunkData.map[x, y, z];
                    if (voxel.properties.isActive) 
                        AddActiveVoxel(voxel);
                }
            }
        }
        
        World.Instance.AddChunkToUpdate(this);

        if (World.Instance.settings.enableAnimatedChunks)
            chunkObject.AddComponent<ChunkLoadAnimation>();
    }
    
    /*public void TickUpdate() {
        if(activeVoxels.Count != 0) 
            Debug.Log(chunkObject.name + " currently has " + activeVoxels.Count + " active blocks.");
        for (var i = activeVoxels.Count - 1; i > -1; i--) {
            if (!BlockBehaviour.Active(activeVoxels[i]))
                RemoveActiveVoxel(activeVoxels[i]);
            else 
                BlockBehaviour.Behave(activeVoxels[i]);
        }
    }*/
    
    public void UpdateChunk()
    {
        ClearMeshData();
        
        for (var y = 0; y < VoxelData.ChunkHeight; y++) {
            for (var x = 0; x < VoxelData.ChunkWidth; x++) {
                for (var z = 0; z < VoxelData.ChunkWidth; z++) {
                    if(World.Instance.blocktypes[chunkData.map[x, y, z].id].isSolid)
                        UpdateMeshData(new Vector3(x, y, z));
                }
            }
        }
        
        World.Instance.chunksToDraw.Enqueue(this);
    }
    
    public void AddActiveVoxel (VoxelState voxel) {
        if (!activeVoxels.Contains(voxel)) // Make sure voxel isn't already in list.
            activeVoxels.Add(voxel);   
    }

    public void RemoveActiveVoxel(VoxelState voxel) {
        for (var i = 0; i < activeVoxels.Count; i++) {
            if (activeVoxels[i] == voxel) {
                activeVoxels.RemoveAt(i);
                return;
            }
        }
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        waterTriangles.Clear();
        uvs.Clear();
        colors.Clear();
        normals.Clear();
    }

    public bool isActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            if(chunkObject != null)
                chunkObject.SetActive(value);   
        }
    }
    
    public void EditVoxel(Vector3 pos, byte newID)
    {
        var xCheck = Mathf.FloorToInt(pos.x);
        var yCheck = Mathf.FloorToInt(pos.y);
        var zCheck = Mathf.FloorToInt(pos.z);

        var position1 = chunkObject.transform.position;
        xCheck -= Mathf.FloorToInt(position1.x);
        zCheck -= Mathf.FloorToInt(position1.z);

        chunkData.ModifyVoxel(new Vector3Int(xCheck, yCheck, zCheck), newID, World.Instance._player.orientation);
         
        UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        var thisVoxel = new Vector3(x, y, z);
        for (var p = 0; p < 6; p++) {
            var currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            if (!chunkData.IsVoxelInChunk((int) currentVoxel.x, (int) currentVoxel.y, (int) currentVoxel.z)) {
                World.Instance.AddChunkToUpdate(World.Instance.GetChunkFromVector3(currentVoxel + position), true);
            }
        }
    }
    
    public VoxelState GetVoxelFromGlobalVector3(Vector3 pos)
    {
        var xCheck = Mathf.FloorToInt(pos.x);
        var yCheck = Mathf.FloorToInt(pos.y);
        var zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return chunkData.map[xCheck, yCheck, zCheck];
    }
    
    bool IsVoxelInChunk (int x, int y, int z) {
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;
        else
            return true;
    }
    
    VoxelState CheckVoxel (Vector3 pos) {
        int x = Mathf.FloorToInt (pos.x);
        int y = Mathf.FloorToInt (pos.y);
        int z = Mathf.FloorToInt (pos.z);

        if (!IsVoxelInChunk(x, y, z))
            return World.Instance.GetVoxelState(pos + position);
        return chunkData.map[x, y, z];
    }
    
    void UpdateMeshData(Vector3 pos) {
        var x = Mathf.FloorToInt(pos.x);
        var y = Mathf.FloorToInt(pos.y);
        var z = Mathf.FloorToInt(pos.z);
        
        var voxel = chunkData.map[x, y, z];
        
        var rot = voxel.orientation switch
        {
            0 => 180f,
            5 => 270f,
            1 => 0f,
            _ => 90f
        };

        for (var p = 0; p < 6; p++)
        {
            var translatedP = p;
            
            if (voxel.orientation != 1) {
                if (voxel.orientation == 0) {
                    if (p == 0) translatedP = 1;
                    else if (p == 1) translatedP = 0;
                    else if (p == 4) translatedP = 5;
                    else if (p == 5) translatedP = 4;
                } else if (voxel.orientation == 5) {
                    if (p == 0) translatedP = 5;
                    else if (p == 1) translatedP = 4;
                    else if (p == 4) translatedP = 0;
                    else if (p == 5) translatedP = 1;
                } else if (voxel.orientation == 4) {
                    if (p == 0) translatedP = 4;
                    else if (p == 1) translatedP = 5;
                    else if (p == 4) translatedP = 1;
                    else if (p == 5) translatedP = 0;
                }
            }

            var neighbour = chunkData.map[x, y, z].neighbours[translatedP];
            if (neighbour != null && neighbour.properties.renderNeighborFaces) {// && neighbour.properties.renderNeighborFaces && !(voxel.properties.isWater && chunkData.map[x, y + 1, z].properties.isWater)) {
                //if (neighbour != null && neighbour.properties.renderNeighborFaces && !(voxel.properties.isWater && chunkData.map[x, y + 1, z].properties.isWater)) {

                var lightLevel = 1f;//neighbour.lightAsFloat;
                var faceVertCount = 0;
            
                for (var i = 0; i < voxel.properties.meshData.faces[p].vertData.Length; i++)
                {
                    var vertData = voxel.properties.meshData.faces[p].GetVertData(i);
                    vertices.Add(pos + vertData.GetRotatedPosition(new Vector3(0, rot, 0)));
                    normals.Add(VoxelData.faceChecks[p]);
                    colors.Add(new Color(0, 0, 0, lightLevel));
                    if (voxel.properties.isWater)
                        uvs.Add(voxel.properties.meshData.faces[p].vertData[i].uv);
                    else
                        AddTexture(voxel.properties.GetTextureID(p), vertData.uv);
                    faceVertCount++;
                }
                
                if (!voxel.properties.transparency) {
                    foreach (var t in voxel.properties.meshData.faces[p].triangles)
                        triangles.Add(vertexIndex + t);
                } else {
                    if (voxel.properties.isWater) {
                        foreach (var t in voxel.properties.meshData.faces[p].triangles)
                            waterTriangles.Add(vertexIndex + t);
                    }
                    else
                    {
                        foreach (var t in voxel.properties.meshData.faces[p].triangles)
                            transparentTriangles.Add(vertexIndex + t);
                    }
                }

                vertexIndex += faceVertCount;   
            }
        }
    }

    public void CreateMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 3;
        
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        mesh.SetTriangles(waterTriangles.ToArray(), 2);
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

        meshFilter.mesh = mesh;
    }

    void AddTexture(int textureId, Vector2 uv)
    {
        float y = textureId / VoxelData.TextureAtlasSizeInBlocks;
        var x = textureId - y * VoxelData.TextureAtlasSizeInBlocks;
 
        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;
        
        x += VoxelData.NormalizedBlockTextureSize * uv.x;
        y += VoxelData.NormalizedBlockTextureSize * uv.y;

        uvs.Add(new Vector2(x, y));
    }
}

public class ChunkCoord {
    public int x;
    public int z;

    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }

    public ChunkCoord(int _x, int _z) {
        x = _x;
        z = _z;
    }

    public ChunkCoord(Vector3 pos)
    {
        var xCheck = Mathf.FloorToInt(pos.x);
        var zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck / VoxelData.ChunkWidth;
        z = zCheck / VoxelData.ChunkWidth;
    }

    public bool Equals(ChunkCoord other)
    {
        if (other == null)
            return false;
        if (other.x == x && other.z == z)
            return true;
        return false;
    }
}
