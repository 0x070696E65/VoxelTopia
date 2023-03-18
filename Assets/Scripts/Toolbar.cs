using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Toolbar : MonoBehaviour
{
    public UIItemSlot[] slots;
    public RectTransform highlight;
    public int slotIndex = 0;

    [SerializeField] private World world;

    private void Start()
    {
        world.OnFinishedLoadWorld += Init;
    }

    private void Init()
    {
        byte index = 2;
        foreach (var s in slots)
        {
            if (world.blocktypes.Length < index + 1)
            {
                var slot = new ItemSlot(s);
            }
            else
            {
                var stack = new ItemStack(index); //, Random.Range(2, 65));
                var slot = new ItemSlot(s, stack);   
            }
            index++;
        }
    }
}
