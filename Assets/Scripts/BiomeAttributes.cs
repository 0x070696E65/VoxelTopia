using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeAttributes", menuName = "MinecraftTutorial /Biome Attributes")]
public class BiomeAttributes : ScriptableObject
{
    [Header("Biome")]
    public string biomeName;
    public int offset;
    public float scale;
    
    public int terrainHeight;
    public float terrainScale;

    public byte surfaceBlock;
    public byte subSurfaceBlock;

    [Header("Major Flora")]
    public int majourFloraIndex;
    public float majorFloraZoneScale = 1.3f;
    [Range(0.1f, 1f)] 
    public float majorFloraZoneThreshold = 0.6f;
    public float majorFloraPlacementScale = 15f;
    [Range(0.1f, 1f)] 
    public float majorFloraPlacementThreshold = 0.8f;
    public bool placeMejorFlora = true;

    public int maxHeight = 12;
    public int minHeight = 5;
    
    public Lode[] loads;
}

[System.Serializable]
public class Lode
{
    public string nodeName;
    public byte blockId;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float threshold;
    public float noiseOffset;
}