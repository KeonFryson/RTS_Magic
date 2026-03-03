// Assets\Inventory\StorageBox.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StorageSlot
{
    public InventoryItem item = null;
}

public class StorageBox : MonoBehaviour
{
    public int Capacity = 20;
    
    [SerializeField]
    private List<StorageSlot> slots = new List<StorageSlot>();

    private StorageBoxUI storageBoxUI;
    private PlayerInputs playerInputs;
 
    private void Awake()
    {
         
        if (slots.Count != Capacity)
        {
            slots.Clear();
            for (int i = 0; i < Capacity; i++)
                slots.Add(new StorageSlot());
        }
    }

    private void Start()
    {
        storageBoxUI = GetComponentInChildren<StorageBoxUI>();
        if (storageBoxUI != null)
        {
            storageBoxUI.Hide();
        }
        playerInputs = PlayerInputs.Instance;
        if (playerInputs != null)
        {
            
            playerInputs.OnClick += HandleClick;
            playerInputs.OnRightClick += HandleRightClck;
        }
    }

    private void OnDestroy()
    {
        if (playerInputs != null)
        {
            playerInputs.OnClick -= HandleClick;
            playerInputs.OnRightClick -= HandleRightClck;
        }
    }

    
    private void HandleClick(Vector3 worldPos)
    {
        Vector2 mouseWorld2D = new Vector2(worldPos.x, worldPos.y);
        RaycastHit2D hit = Physics2D.Raycast(mouseWorld2D, Vector2.zero);
        if (hit.collider != null && hit.collider.gameObject == gameObject)
        {
            if (storageBoxUI != null)
                storageBoxUI.Show(this);
            else
                Debug.LogWarning("StorageBoxUI component not found in children!");
        }
    }
    private void HandleRightClck(Vector3 worldPos)
    {
        if (storageBoxUI != null)
            storageBoxUI.Hide();
    }

    public bool AddItem(InventoryItem item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].item != null && slots[i].item.itemData.itemID == item.itemData.itemID)
            {
                slots[i].item.itemData.quantity += item.itemData.quantity;
                return true;
            }
        }
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].item == null)
            {
                InventoryItem newItem = ScriptableObject.CreateInstance<InventoryItem>();
                newItem.itemData = new ItemData
                {
                    itemName = item.itemData.itemName,
                    description = item.itemData.description,
                    itemIcon = item.itemData.itemIcon,
                    itemID = item.itemData.itemID,
                    quantity = item.itemData.quantity,
                    maxStackSize = item.itemData.maxStackSize,
                    buildingPrefab = item.itemData.buildingPrefab,
                    placementMask = item.itemData.placementMask,
                };
                slots[i].item = newItem;
                return true;
            }
        }
        return false;
    }

    public bool AddItemByID(int itemId, int amount)
    {
        var template = ItemDatabase.Instance != null
            ? ItemDatabase.Instance.GetInventoryItemByID(itemId)
            : null;
        if (template == null)
            return false;

        InventoryItem newItem = ScriptableObject.CreateInstance<InventoryItem>();
        newItem.itemData = new ItemData
        {
            itemName = template.itemData.itemName,
            description = template.itemData.description,
            itemIcon = template.itemData.itemIcon,
            itemID = template.itemData.itemID,
            quantity = amount,
            maxStackSize = template.itemData.maxStackSize,
            buildingPrefab = template.itemData.buildingPrefab,
            placementMask = template.itemData.placementMask,
        };
        return AddItem(newItem);
    }

    public bool RemoveItem(int itemId, int amount)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].item != null && slots[i].item.itemData.itemID == itemId && slots[i].item.itemData.quantity >= amount)
            {
                slots[i].item.itemData.quantity -= amount;
                if (slots[i].item.itemData.quantity == 0)
                    slots[i].item = null;
                return true;
            }
        }
        return false;
    }

    public List<StorageSlot> GetSlots()
    {
        return slots;
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count || indexB < 0 || indexB >= slots.Count)
            return;
        var temp = slots[indexA].item;
        slots[indexA].item = slots[indexB].item;
        slots[indexB].item = temp;
    }
}