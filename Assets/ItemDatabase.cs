using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public static ItemDatabase Instance { get; private set; }

    public InventoryItem[] allItems;

    private void OnEnable()
    {
        Instance = this;
        // Automatically populate allItems at runtime from Resources/Items
        PopulateFromResources();
    }

    public void PopulateFromResources()
    {
        // Loads all InventoryItem assets in Resources/Items (recursively)
        allItems = Resources.LoadAll<InventoryItem>("Items");
        Debug.Log($"[ItemDatabase] Loaded {allItems.Length} items from Resources/Items.");
    }

    public InventoryItem GetInventoryItemByID(int id)
    {
        foreach (var item in allItems)
        {
            if (item.itemData != null && item.itemData.itemID == id)
                return item;
        }
        return null;
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh Items From Resources")]
    public void EditorRefresh()
    {
        PopulateFromResources();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}