using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    private World world;
    private Text text;

    private float frameRate;
    private float timer;

    private int halfWorldSizeInVoxels;
    //private int halfWorldSizeInChunks;
    
    void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<Text>();

        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        //halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;
    }
    
    void Update()
    {
        var debugText = "";
        debugText += frameRate + " fps";
        debugText += "\n";
        debugText += "XYZ: " + (Mathf.FloorToInt(world.player.transform.position.x) - halfWorldSizeInVoxels) + " / " +
                     Mathf.FloorToInt(world.player.transform.position.y) + " / " +
                     (Mathf.FloorToInt(world.player.transform.position.z) - halfWorldSizeInVoxels);
        text.text = debugText;

        if (timer > 1f) {
            frameRate = (int) (1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
        {
            timer += Time.deltaTime;
        }
    }
}
