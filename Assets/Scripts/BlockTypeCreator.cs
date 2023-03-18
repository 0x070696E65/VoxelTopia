using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlockTypeCreator: MonoBehaviour
{
    private static int blockSize = 16; // Block size in pixels.
    private static int atlasSizeInBlocks = 16;
    private static int atlasSize;
    private static List<Texture2D> sortedTextures = new ();
    public static List<Texture2D> rawTextures = new ();

    public static Dictionary<string, Dictionary<string, byte[]>> blockTypeListData = new();

    public static Texture mainTexture;
    public static BlockType[] blocktypes;
    public static List<Texture2D> iconTextureList = new ();
    public static List<Sprite> iconSpriteList = new ();

    [SerializeField] private Texture2D bedrock;
    
    public static async Task SetBlockTypes(string worldName)
    {
        var filePath = $"{Application.persistentDataPath}/worlds/{worldName}/";
        var blockTypesStr = await File.ReadAllTextAsync(filePath + "blockTypes.json");
        var _blockTypesForInventory = JsonUtility.FromJson<BlockTypesForInventory>(blockTypesStr);
        
        var atlas = await File.ReadAllBytesAsync(filePath + "atlas.png");
        var texture = LoadPNG(256, atlas);
        mainTexture = texture;
        
        blocktypes = new BlockType[_blockTypesForInventory.blockTypes.Count];
        for (var i = 0; i < _blockTypesForInventory.blockTypes.Count; i++)
        {
            var fileData = await File.ReadAllBytesAsync($"{filePath}icons/{_blockTypesForInventory.blockTypes[i].iconName}");
            var _texture = LoadPNG(64, fileData);
            var _sprite = Sprite.Create(_texture, new Rect(0, 0, _texture.width, _texture.height), Vector2.zero);
            iconTextureList.Add(_texture);
            iconSpriteList.Add(_sprite);
            
            blocktypes[i] = new BlockType
            {
                blockName = _blockTypesForInventory.blockTypes[i].voxelName,
                transparency = _blockTypesForInventory.blockTypes[i].transparency,
                renderNeighborFaces = _blockTypesForInventory.blockTypes[i].transparency,
                isSolid = _blockTypesForInventory.blockTypes[i].isSolid,
                isWater = _blockTypesForInventory.blockTypes[i].isWater,
                meshData = Resources.Load<VoxelMeshData>("StandardBlock"),
                icon = _sprite,
                backFaceTexture = _blockTypesForInventory.blockTypes[i].faceTextures[0],
                frontFaceTexture = _blockTypesForInventory.blockTypes[i].faceTextures[1],
                topFaceTexture = _blockTypesForInventory.blockTypes[i].faceTextures[2],
                bottomFaceTexture = _blockTypesForInventory.blockTypes[i].faceTextures[3],
                leftFaceTexture = _blockTypesForInventory.blockTypes[i].faceTextures[4],
                rightFaceTexture = _blockTypesForInventory.blockTypes[i].faceTextures[5]
            };
        }
    }
    
    public async Task CreateBlockTypes(string savePath, List<string> blockPaths)
    {
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            Directory.CreateDirectory($"{savePath}/icons");   
        }
        var voxlePath = Application.persistentDataPath + "/voxels/";

        var blockTypeForInventories = new BlockTypesForInventory();

        byte faceCounter = 1;
        foreach (var p in blockPaths)
        {
            var blockTypeForInventory = new BlockTypeForInventory();
            var arr = p.Split("_");
            blockTypeForInventory.voxelName = arr[0];
            blockTypeForInventory.metalId = arr.Length > 1 ? arr[1] : "";
            var vdata = JsonUtility.FromJson<Vdata>(await File.ReadAllTextAsync(voxlePath + p + "/data.json"));
            blockTypeForInventory.meshDataType = vdata.type;
            blockTypeForInventory.transparency = true;
            blockTypeForInventory.isSolid = true;
            blockTypeForInventory.isWater = false;
            blockTypeForInventory.iconName = p + ".png";
            blockTypeForInventory.transparency = vdata.transparency;
            
            blockTypeListData[p] = new Dictionary<string, byte[]>();
            var s = Directory.GetFiles(voxlePath + p + "/", "*.png", SearchOption.TopDirectoryOnly);

            foreach (var sss in s)
            {
                if (Path.GetFileName(sss).Contains("icon"))
                {
                    await File.WriteAllBytesAsync($"{savePath}/icons/{p}.png", await File.ReadAllBytesAsync(sss));
                }
                else
                {
                    var textureId = Path.GetFileName(sss).Replace(".png", "");
                    blockTypeListData[p][textureId] = await File.ReadAllBytesAsync(sss);
                    for (var i = 0; i < vdata.face.Length; i++)
                    {
                        if (vdata.face[i].textureId == textureId)
                        {
                            blockTypeForInventory.faceTextures[i] = faceCounter;
                        }
                    }
                    faceCounter++;
                }
            }
            blockTypeForInventories.blockTypes.Add(blockTypeForInventory);
        }
        
        var blockTypeForInventoriesJson = JsonUtility.ToJson(blockTypeForInventories);
        
        await File.WriteAllTextAsync($"{savePath}/blockTypes.json", blockTypeForInventoriesJson);
        
        var atlas = PackAtlas();
        var bytes = atlas.EncodeToPNG();
        await File.WriteAllBytesAsync($"{savePath}/atlas.png", bytes);
    }
    
    static void LoadTextures()
    {
        sortedTextures.Clear();
        rawTextures.Clear();
        foreach (var keyValuePair in blockTypeListData.Values.SelectMany(bytesMap => bytesMap))
        {
            rawTextures.Add(LoadPNG(blockSize, keyValuePair.Value));
        }
        var index = 0;
        foreach (var tex in rawTextures)
        {
            if(tex.width == blockSize && tex.height == blockSize)
                sortedTextures.Add(tex);
            else
                Debug.Log("Asset Packer: " + tex.name + " incorrect size. Texture not loaded");

            index++;
        }
        Debug.Log("Atlas Packer: " + sortedTextures.Count + " successfully loaded.");
    }
    
    public Texture2D PackAtlas()
    {
        LoadTextures();
        sortedTextures.Insert(0, bedrock);
        atlasSize = blockSize * atlasSizeInBlocks;
        
        var atlas = new Texture2D(atlasSize, atlasSize);
        var pixels = new Color[atlasSize * atlasSize];

        for (var x = 0; x < atlasSize; x++) {
            for (var y = 0; y < atlasSize; y++) {
                // Get the current block that we're looking at.
                var currentBlockX = x / blockSize;
                var currentBlockY = y / blockSize;

                var index = currentBlockY * atlasSizeInBlocks + currentBlockX;
                
                if (index < sortedTextures.Count)
                    pixels[(atlasSize - y - 1) * atlasSize + x] = sortedTextures[index].GetPixel(x, blockSize - y - 1);
                else
                    pixels[(atlasSize - y - 1) * atlasSize + x] = new Color(0f, 0f, 0f, 0f);
            }   
        }
        
        atlas.SetPixels(pixels);
        atlas.Apply();
        return atlas;
    }
    
    public static Texture2D LoadPNG(int size, byte[] fileData) {
        var texture = new Texture2D(size, size);
        texture.LoadImage(fileData);
        var change_pixels = new Color[size * size];
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                change_pixels[x + y * size] = texture.GetPixel(x, y);
            }
        }
        Destroy(texture);
        var change_texture = new Texture2D (size, size) {
            filterMode = FilterMode.Point
        };
        change_texture.SetPixels(change_pixels);
        change_texture.Apply();
        return change_texture;
    }
}

[Serializable]
public class BlockTypeForInventory
{
    public string voxelName;
    public string metalId;
    public bool isSolid;
    public int meshDataType;
    public bool transparency;
    public bool isWater;
    public string iconName;
    public byte[] faceTextures = new byte[6];
}

[Serializable]
public class BlockTypesForInventory
{
    public List<BlockTypeForInventory> blockTypes = new ();
}
