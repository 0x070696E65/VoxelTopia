using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreativeInventory : MonoBehaviour
{
    public GameObject slotPrefab;
    [SerializeField] private World world;

    private List<ItemSlot> slots = new List<ItemSlot>();
    
    private void Start()
    {
        for (var i = 2; i < world.blocktypes.Length; i++)
        {
            var newSlot = Instantiate(slotPrefab, transform);
            var stack = new ItemStack((byte) i); //, 64);
            var slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(), stack);
            slot.isCreative = true;
        }
    }
}
