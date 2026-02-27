using System;
using UnityEngine;

[Serializable]
public class ItemData
{
    public string itemName;
    public Sprite itemIcon;
    public int itemID;
    public int quantity;

    public int maxStackSize;

    public string description;
    public bool isConsumable;

}
