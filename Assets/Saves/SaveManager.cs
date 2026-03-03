using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class PlacedBuildingData
{
    public int itemID;
    public float x, y, z;
}

 
public static class SaveManager
{
    private const string DestroyedNodesKey = "DestroyedResourceNodes";
    private const string InventoryKey = "PlayerInventory";
    private const string PlacedBuildingsKey = "PlacedBuildings";
    private const string PlayerPosKey = "PlayerPosition";
 
    private static HashSet<Vector2Int> destroyedNodes = new HashSet<Vector2Int>();
    private static List<PlacedBuildingData> placedBuildings = new List<PlacedBuildingData>();

    // Control saving in editor
    public static bool AllowEditorSave { get; set; } = false;

   
    // --- Player Position ---
    public static void SavePlayerPosition(Vector3 pos)
    {
        PlayerPrefs.SetString(PlayerPosKey, $"{pos.x}|{pos.y}|{pos.z}");
    }

    public static Vector3? LoadPlayerPosition()
    {
        var data = PlayerPrefs.GetString(PlayerPosKey, "");
        if (string.IsNullOrEmpty(data))
            return null;
        var parts = data.Split('|');
        if (parts.Length == 3 &&
            float.TryParse(parts[0], out float x) &&
            float.TryParse(parts[1], out float y) &&
            float.TryParse(parts[2], out float z))
        {
            return new Vector3(x, y, z);
        }
        return null;
    }

    // --- Placed Buildings ---
    public static void AddPlacedBuilding(int itemID, Vector3 position)
    {
        placedBuildings.Add(new PlacedBuildingData
        {
            itemID = itemID,
            x = position.x,
            y = position.y,
            z = position.z
        });
    }

    public static void SavePlacedBuildings()
    {
        var data = placedBuildings
            .Select(b => $"{b.itemID}|{b.x}|{b.y}|{b.z}")
            .ToArray();
        PlayerPrefs.SetString(PlacedBuildingsKey, string.Join(";", data));
    }

    public static void LoadPlacedBuildings()
    {
        placedBuildings.Clear();
        var data = PlayerPrefs.GetString(PlacedBuildingsKey, "");
        if (string.IsNullOrEmpty(data))
            return;
        var entries = data.Split(';');
        foreach (var entry in entries)
        {
            var parts = entry.Split('|');
            if (parts.Length == 4 &&
                int.TryParse(parts[0], out int itemID) &&
                float.TryParse(parts[1], out float x) &&
                float.TryParse(parts[2], out float y) &&
                float.TryParse(parts[3], out float z))
            {
                placedBuildings.Add(new PlacedBuildingData
                {
                    itemID = itemID,
                    x = x,
                    y = y,
                    z = z
                });
            }
        }
    }

    public static List<PlacedBuildingData> GetPlacedBuildings()
    {
        return placedBuildings;
    }

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
        var list = new List<string>();
        foreach (var item in items)
        {
            if (item == null || item.itemData == null) continue;
            var data = $"{item.itemData.itemID}|{item.itemData.quantity}";
            list.Add(data);
        }
        PlayerPrefs.SetString(InventoryKey, string.Join(";", list));
    }

    public static void LoadInventory(Inventory inventory)
    {
        var data = PlayerPrefs.GetString(InventoryKey, "");
        if (string.IsNullOrEmpty(data))
        {
            Debug.Log("[SaveManager] No inventory data found to load.");
            return;
        }

        var entries = data.Split(';');
        inventory.ClearInventory();
        foreach (var entry in entries)
        {
            var parts = entry.Split('|');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int itemID) &&
                int.TryParse(parts[1], out int qty))
            {
                InventoryItem itemTemplate = ItemDatabase.Instance.GetInventoryItemByID(itemID);
                if (itemTemplate != null)
                {
                    InventoryItem item = ScriptableObject.CreateInstance<InventoryItem>();
                    item.itemData = new ItemData
                    {
                        itemID = itemTemplate.itemData.itemID,
                        itemName = itemTemplate.itemData.itemName,
                        itemIcon = itemTemplate.itemData.itemIcon,
                        maxStackSize = itemTemplate.itemData.maxStackSize,
                        description = itemTemplate.itemData.description,
                        isConsumable = itemTemplate.itemData.isConsumable,
                        quantity = qty,
                        buildingPrefab = itemTemplate.itemData.buildingPrefab,
                        placementMask = itemTemplate.itemData.placementMask
                    };
                    inventory.AddItem(item);
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
        SavePlacedBuildings();
        SavePlayerPosition(GameObject.FindWithTag("Player").transform.position);
       ;
        PlayerPrefs.Save();
    }

    public static void LoadAll()
    {
        LoadDestroyedNodes();
        if (Inventory.Instance != null)
            LoadInventory(Inventory.Instance);
        else
            Debug.LogWarning("[SaveManager] Inventory instance not found during LoadAll.");
        LoadPlacedBuildings();
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