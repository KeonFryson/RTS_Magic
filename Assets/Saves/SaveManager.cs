using System.Collections.Generic;
using UnityEngine;

public static class SaveManager
{
    private const string DestroyedNodesKey = "DestroyedResourceNodes";
    private const string InventoryKey = "PlayerInventory";
    private static HashSet<Vector2Int> destroyedNodes = new HashSet<Vector2Int>();

    // Control saving in editor
    public static bool AllowEditorSave { get; set; } = false;

    
    // --- Destroyed Resource Nodes ---
    public static void MarkResourceNodeDestroyed(Vector2Int tilePos)
    {
        destroyedNodes.Add(tilePos);
    }

    public static bool IsResourceNodeDestroyed(Vector2Int tilePos)
    {
        return destroyedNodes.Contains(tilePos);
    }

    // --- Inventory ---
    public static void SaveInventory(InventoryItem[] items)
    {
        //Debug.Log($"[SaveManager] Saving inventory. Item slots: {items.Length}");
        var list = new List<string>();
        int savedCount = 0;
        foreach (var item in items)
        {
            if (item == null || item.itemData == null) continue;
            // Save itemID and quantity
            var data = $"{item.itemData.itemID}|{item.itemData.quantity}";
            list.Add(data);
            //Debug.Log($"[SaveManager] Saving item: ID={item.itemData.itemID}, Qty={item.itemData.quantity}");
            savedCount++;
        }
        //Debug.Log($"[SaveManager] Total items saved: {savedCount}");
        PlayerPrefs.SetString(InventoryKey, string.Join(";", list));
    }

    public static void LoadInventory(Inventory inventory)
    {
       // Debug.Log("[SaveManager] LoadInventory called");
        var data = PlayerPrefs.GetString(InventoryKey, "");
        if (string.IsNullOrEmpty(data))
        {
            Debug.Log("[SaveManager] No inventory data found to load.");
            return;
        }

        var entries = data.Split(';');
       // Debug.Log($"[SaveManager] Loading inventory. Entries found: {entries.Length}");
        inventory.ClearInventory();
        int loadedCount = 0;
        foreach (var entry in entries)
        {
            var parts = entry.Split('|');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int itemID) &&
                int.TryParse(parts[1], out int qty))
            {
                // Look up InventoryItem by itemID
                InventoryItem itemTemplate = ItemDatabase.Instance.GetInventoryItemByID(itemID);
                if (itemTemplate != null)
                {
                    // Create a new instance to avoid modifying the asset
                    InventoryItem item = ScriptableObject.CreateInstance<InventoryItem>();
                    item.itemData = new ItemData
                    {
                        itemID = itemTemplate.itemData.itemID,
                        itemName = itemTemplate.itemData.itemName,
                        itemIcon = itemTemplate.itemData.itemIcon,
                        maxStackSize = itemTemplate.itemData.maxStackSize,
                        description = itemTemplate.itemData.description,
                        isConsumable = itemTemplate.itemData.isConsumable,
                        quantity = qty
                    };
                    inventory.AddItem(item);
                   // Debug.Log($"[SaveManager] Loaded item: ID={itemID}, Qty={qty}");
                    loadedCount++;
                }
                else
                {
                    Debug.LogWarning($"[SaveManager] InventoryItem not found for ID: {itemID}");
                }
            }
            else
            {
                Debug.LogWarning($"[SaveManager] Failed to parse inventory entry: {entry}");
            }
        }
       // Debug.Log($"[SaveManager] Total items loaded: {loadedCount}");
        inventory.OnInventoryChanged?.Invoke();
    }

    // --- Save/Load All ---
    public static void SaveAll()
    {
#if UNITY_EDITOR
        if (Application.isEditor && !AllowEditorSave)
            return;
#endif
        SaveDestroyedNodes();
        if (Inventory.Instance != null)
            SaveInventory(Inventory.Instance.GetItems());
        PlayerPrefs.Save();
    }

    public static void LoadAll()
    {
        LoadDestroyedNodes();
        if (Inventory.Instance != null)
            LoadInventory(Inventory.Instance);
        else
            Debug.LogWarning("[SaveManager] Inventory instance not found during LoadAll.");
    }

    // --- Internal Save/Load for Destroyed Nodes ---
    private static void SaveDestroyedNodes()
    {
        var list = new List<string>();
        foreach (var pos in destroyedNodes)
            list.Add($"{pos.x}_{pos.y}");
        PlayerPrefs.SetString(DestroyedNodesKey, string.Join(",", list));
    }

    private static void LoadDestroyedNodes()
    {
        destroyedNodes.Clear();
        var data = PlayerPrefs.GetString(DestroyedNodesKey, "");
        if (string.IsNullOrEmpty(data))
            return;
        var entries = data.Split(',');
        foreach (var entry in entries)
        {
            var parts = entry.Split('_');
            if (parts.Length == 2 && int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
                destroyedNodes.Add(new Vector2Int(x, y));
        }
    }
}