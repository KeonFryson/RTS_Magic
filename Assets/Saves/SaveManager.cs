using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

[System.Serializable]
public class PlacedBuildingData
{
    public int itemID;
    public float x, y, z;
}

[System.Serializable]
public class SaveData
{
    public List<PlacedBuildingData> placedBuildings = new List<PlacedBuildingData>();
    public List<Vector2Int> destroyedNodes = new List<Vector2Int>();
    public List<InventoryItemData> inventory = new List<InventoryItemData>();
    public Vector3 playerPosition;
    public bool hasPlayerPosition = false;
}

[System.Serializable]
public class InventoryItemData
{
    public int itemID;
    public int quantity;
}

public static class SaveManager
{
    private static readonly string SaveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");

    private static HashSet<Vector2Int> destroyedNodes = new HashSet<Vector2Int>();
    private static List<PlacedBuildingData> placedBuildings = new List<PlacedBuildingData>();

    public static bool AllowEditorSave { get; set; } = false;

    // --- Player Position ---
    public static void SavePlayerPosition(Vector3 pos)
    {
        Debug.Log($"[SaveManager] SavePlayerPosition: Saving position {pos}");
        saveData.playerPosition = pos;
        saveData.hasPlayerPosition = true;
    }

    public static Vector3? LoadPlayerPosition()
    {
        if (saveData.hasPlayerPosition)
        {
            Debug.Log($"[SaveManager] LoadPlayerPosition: playerPosition={saveData.playerPosition}");
            return saveData.playerPosition;
        }
        Debug.Log("[SaveManager] LoadPlayerPosition: No player position saved.");
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
        saveData.inventory.Clear();
        foreach (var item in items)
        {
            if (item == null || item.itemData == null) continue;
            saveData.inventory.Add(new InventoryItemData
            {
                itemID = item.itemData.itemID,
                quantity = item.itemData.quantity
            });
        }
    }

    public static void LoadInventory(Inventory inventory)
    {
        inventory.ClearInventory();
        if (saveData.inventory == null)
            saveData.inventory = new List<InventoryItemData>();

        if (ItemDatabase.Instance == null)
        {
            Debug.LogError("[SaveManager] ItemDatabase.Instance is null! Cannot load inventory. Make sure ItemDatabase is initialized before loading inventory.");
            return;
        }

        foreach (var itemData in saveData.inventory)
        {
            InventoryItem itemTemplate = ItemDatabase.Instance.GetInventoryItemByID(itemData.itemID);
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
                    quantity = itemData.quantity,
                    buildingPrefab = itemTemplate.itemData.buildingPrefab,
                    placementMask = itemTemplate.itemData.placementMask
                };
                inventory.AddItem(item);
            }
            else
            {
                Debug.LogWarning($"[SaveManager] InventoryItem not found for ID: {itemData.itemID}");
            }
        }
        inventory.OnInventoryChanged?.Invoke();
    }


    // --- Save/Load All ---
    private static SaveData saveData = new SaveData();

    public static void SaveAll()
    {
#if UNITY_EDITOR
        if (Application.isEditor && !AllowEditorSave)
            return;
#endif
        saveData.destroyedNodes = destroyedNodes.ToList();
        saveData.placedBuildings = placedBuildings.ToList();
        if (Inventory.Instance != null)
            SaveInventory(Inventory.Instance.GetItems());
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            SavePlayerPosition(playerObj.transform.position);
        else
             Debug.LogWarning("[SaveManager] Player object not found during SaveAll.");

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SaveFilePath, json);
    }

    public static void LoadAll()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("[SaveManager] No save file found.");
            saveData = new SaveData(); // Ensure saveData is initialized
            return;
        }
        string json = File.ReadAllText(SaveFilePath);
        saveData = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();

        // Ensure lists are initialized to avoid null references
        saveData.destroyedNodes ??= new List<Vector2Int>();
        saveData.placedBuildings ??= new List<PlacedBuildingData>();
        saveData.inventory ??= new List<InventoryItemData>();

        destroyedNodes = new HashSet<Vector2Int>(saveData.destroyedNodes);
        placedBuildings = saveData.placedBuildings;

        Debug.Log($"[SaveManager] LoadAll: Player position loaded as {saveData.playerPosition}");

        if (Inventory.Instance != null)
            LoadInventory(Inventory.Instance);
        else
            Debug.LogWarning("[SaveManager] Inventory instance not found during LoadAll.");
    }
}