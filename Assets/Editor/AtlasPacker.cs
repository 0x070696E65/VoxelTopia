using System.Collections;
using System.Collections.Generic;
using System.IO;
using Codice.Utils;
using UnityEditor;
using UnityEngine;

public class AtlasPacker : EditorWindow
{
    private int blockSize = 16; // Block size in pixels.
    private int atlasSizeInBlocks = 16;
    private int atlasSize;

    private Object[] rawTextures = new Object[256];
    private List<Texture2D> sortedTextures = new List<Texture2D>();
    private Texture2D atlas;
    
    [MenuItem("Minecraft Clone/AtlasPacker")]
    
    public static void ShowWindow()
    {
        GetWindow(typeof(AtlasPacker));
    }

    private void OnGUI()
    {
        atlasSize = blockSize * atlasSizeInBlocks;
        GUILayout.Label("Minecraft Cone Texture Atlas Packer", EditorStyles.boldLabel);

        blockSize = EditorGUILayout.IntField("Block Size", blockSize);
        atlasSizeInBlocks = EditorGUILayout.IntField("Atlas Size (in blocks)", blockSize);

        GUILayout.Label(atlas);
        
        if (GUILayout.Button("Load Textures"))
        {
            LoadTextures();
            PackAtlas();
            
            Debug.Log("Atlas Packer: Textures loaded.");
        }

        if (GUILayout.Button("Clear Textures"))
        {
            atlas = new Texture2D(atlasSize, atlasSize);
            Debug.Log("Atlas Packer: Textures cleared.");
        }

        if (GUILayout.Button("Save Atlas"))
        {
            var bytes = atlas.EncodeToPNG();

            try
            {
                File.WriteAllBytes(Application.dataPath + "/Textures/Packed_atlas.png", bytes);
            }
            catch
            {
                Debug.Log("Atlas Packer: Couldn't save atlas to file.");
            }
        }
    }

    void LoadTextures()
    {
        sortedTextures.Clear();
        rawTextures = Resources.LoadAll("AtlasPacker", typeof(Texture2D));
        var index = 0;
        foreach (var tex in rawTextures)
        {
            var t = (Texture2D) tex;
            if(t.width == blockSize && t.height == blockSize)
                sortedTextures.Add(t);
            else
                Debug.Log("Asset Packer: " + tex.name + " incorrect size. Texture not loaded");

            index++;
        }
        Debug.Log("Atlas Packer: " + sortedTextures.Count + " successfully loaded.");
    }

    private void PackAtlas()
    {
        atlas = new Texture2D(atlasSize, atlasSize);
        var pixels = new Color[atlasSize * atlasSize];

        for (var x = 0; x < atlasSize; x++) {
            for (var y = 0; y < atlasSize; y++) {
                // Get the current block that we're looking at.
                var currentBlockX = x / blockSize;
                var currentBlockY = y / blockSize;

                var index = currentBlockY * atlasSizeInBlocks + currentBlockX;
                
                // Get the pixel in the current block.
                var currentPixelX = x - currentBlockX * blockSize;
                var currentPixelY = y - currentBlockY * blockSize;

                if (index < sortedTextures.Count)
                    pixels[(atlasSize - y - 1) * atlasSize + x] = sortedTextures[index].GetPixel(x, blockSize - y - 1);
                else
                    pixels[(atlasSize - y - 1) * atlasSize + x] = new Color(0f, 0f, 0f, 0f);
            }   
        }
        
        atlas.SetPixels(pixels);
        atlas.Apply( );
    }
}
