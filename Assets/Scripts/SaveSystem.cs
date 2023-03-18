using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

public static class SaveSystem
{
    public static async Task SaveWorld(string worldName, string data)
    {
        // Set our save location and make sure we have a saves folder ready to go.
        var savePath = World.Instance.appPath + "/worlds/" + worldName + "/";
        
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);
        
        await File.WriteAllTextAsync(savePath + "world.json", data);
        Debug.Log("Saving " + worldName);
    }

    public static void SaveChunks(WorldData world)
    {
        var chunks = new List<ChunkData>(world.modifiedChnks);
        world.modifiedChnks.Clear();

        var count = 0;
        foreach (var chunk in chunks)
        {
            SaveChunk(chunk, world.worldName);
            count++;
        }

        Debug.Log(count + " chunks saved.");
    }

    public static async Task<string> LoadWorld(string worldName, int seed = 0)
    {
        var loadPath = World.Instance.appPath + "/worlds/" + worldName + "/world.json";
        if (File.Exists(loadPath))
            return await File.ReadAllTextAsync(loadPath);
        Debug.Log(worldName + " not found. Creating new world.");
        var world = new WorldData(worldName, seed);
        await SaveWorld(world.worldName, "");
        return "";
    }
    
    private static void SaveChunk(ChunkData chunk, string worldName)
    {
        var chunkName = chunk.position.x + "-" + chunk.position.y;
        
        // Set our save location and make sure we have a saves folder ready to go.
        var savePath = World.Instance.appPath + "/saves/" + worldName + "/chunks/";
        
        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);
        
        var formatter = new BinaryFormatter();
        var stream = new FileStream(savePath + chunkName +".chunk", FileMode.Create);
        
        formatter.Serialize(stream, chunk);
        stream.Close();
    }
    
    public static ChunkData LoadChunk(string worldName, Vector2Int position)
    {
        var chunkName = position.x + "-" + position.y;
        
        var loadPath = World.Instance.appPath + "/saves/" + worldName + "/chunks/" + chunkName + ".chunk";
        if (!File.Exists(loadPath)) return null;
        
        var formatter = new BinaryFormatter();
        var stream = new FileStream(loadPath, FileMode.Open);

        var chunkData = formatter.Deserialize(stream) as ChunkData;
        stream.Close();
        return chunkData;
    }
}
