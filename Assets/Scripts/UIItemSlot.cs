using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItemSlot : MonoBehaviour
{
    public bool isLinked = false;
    public ItemSlot itemSlot;
    public Image slotImage;
    public Image slotIcon;
    // public Text slotAmount;

    private World world;

    public bool HasItem => itemSlot is {HasItem: true};
    
    private void Awake() {
        world = GameObject.Find("World").GetComponent<World>();
    }
    
    public void Link(ItemSlot _itemSlot)
    {
        itemSlot = _itemSlot;
        isLinked = true;
        itemSlot.LinkUISlot(this);
        UpdateSlot();
    }

    public void UnLink()
    {
        itemSlot.UnLinkUISlot();
        itemSlot = null;
        UpdateSlot();
    }

    public void UpdateSlot()
    {
        if (itemSlot != null && itemSlot.HasItem)
        {
            slotIcon.sprite = world.blocktypes[itemSlot.stack.id].icon;
            // slotAmount.text = itemSlot.stack.amount.ToString();
            slotIcon.enabled = true;
            // slotAmount.enabled = true;
        }
        else
        {
            Clear();
        }
    }

    public void Clear()
    {
        slotIcon.sprite = null;
        // slotAmount.text = "";
        slotIcon.enabled = false;
        // slotAmount.enabled = false;
    }

    private void OnDestroy()
    {
        if(itemSlot != null)
            itemSlot.UnLinkUISlot();
    }
}

public class ItemSlot
{
    public ItemStack stack;
    private UIItemSlot uiItemSlot = null;

    public bool isCreative; 

    public ItemSlot(UIItemSlot _uiItemSlot)
    {
        stack = null;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public ItemSlot(UIItemSlot _uiItemSlot, ItemStack _stack)
    {
        stack = _stack;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public void LinkUISlot(UIItemSlot uiSlot)
    {
        uiItemSlot = uiSlot;
    }

    public void UnLinkUISlot()
    {
        uiItemSlot = null;
    }

    public void EmptySlot()
    {
        stack = null;
        if(uiItemSlot != null)
            uiItemSlot.UpdateSlot();
    }

    public void Take(int amt)
    {
        /*if (amt > stack.amount)
        {
            EmptySlot();
            return;
        }
        if (amt < stack.amount)
        {
            stack.amount -= amt;
            uiItemSlot.UpdateSlot();
            return;
        }*/
        EmptySlot();
    }

    public ItemStack TakeAll()
    {
        var handOver = new ItemStack(stack.id); //, stack.amount);
        EmptySlot();
        return handOver;
    }

    public void InsertStack(ItemStack _stack)
    {
        stack = _stack;
        uiItemSlot.UpdateSlot();
    }
    public bool HasItem => stack != null;
}