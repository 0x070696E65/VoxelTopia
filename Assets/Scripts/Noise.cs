using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float Get2DPerlin(Vector2 position, float offset, float scale)
    {
        position.x += offset + VoxelData.seed + 0.1f;
        position.y += offset + VoxelData.seed + 0.1f;
        
        return Mathf.PerlinNoise(position.x / VoxelData.ChunkWidth * scale,position.y / VoxelData.ChunkWidth * scale);
    }

    public static bool Get3DPeriln(Vector3 position, float offset, float scale, float threshold)
    {
        var x = (position.x + offset + VoxelData.seed + 0.1f) * scale;
        var y = (position.y + offset + VoxelData.seed + 0.1f) * scale;
        var z = (position.z + offset + VoxelData.seed + 0.1f) * scale;

        var AB = Mathf.PerlinNoise(x, y);
        var BC = Mathf.PerlinNoise(y, z);
        var AC = Mathf.PerlinNoise(x, z);
        var BA = Mathf.PerlinNoise(y, x);
        var CB = Mathf.PerlinNoise(z, y);
        var CA = Mathf.PerlinNoise(z, x);

        if ((AB + BC + AC + BA + CB + CA) / 6f > threshold)
            return true;
        return false;
    }
}