// Assets\Inventory\StorageBox.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class StorageSlot
{
    public InventoryItem item = null;
}

public class StorageBox : MonoBehaviour
{
    public int Capacity = 20;
    public string boxID;
    [SerializeField]
    private List<StorageSlot> slots = new List<StorageSlot>();

    private StorageBoxUI storageBoxUI;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(boxID))
            boxID = System.Guid.NewGuid().ToString();
    }
#endif
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
        // Example: Add some items to the storage box for testing
       
        storageBoxUI = GetComponentInChildren<StorageBoxUI>();
        if (storageBoxUI != null)
        {
            storageBoxUI.Hide();
        }
    }



    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (storageBoxUI != null)
                    storageBoxUI.Show(this);
                else
                    Debug.LogWarning("StorageBoxUI component not found in children!");
            }
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame && storageBoxUI != null && storageBoxUI.gameObject.activeSelf)
        {
            storageBoxUI.Hide();
        }
    }

    public bool AddItem(InventoryItem item)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].item != null && slots[i].item.itemData.itemID == item.itemData.itemID)
            {
                slots[i].item.itemData.quantity += item.itemData.quantity;
               // Debug.Log($"[StorageBox] Stacked itemID {item.itemData.itemID}, new quantity: {slots[i].item.itemData.quantity} in box {boxID}");
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
               // Debug.Log($"[StorageBox] Added itemID {item.itemData.itemID}, quantity: {item.itemData.quantity} to slot {i} in box {boxID}");
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