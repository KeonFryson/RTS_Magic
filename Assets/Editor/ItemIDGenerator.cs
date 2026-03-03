using UnityEngine;
using UnityEditor;

public static class ItemIDGenerator
{
    [MenuItem("Tools/Inventory/Auto Assign ItemIDs")]
    public static void AutoAssignItemIDs()
    {
        string[] guids = AssetDatabase.FindAssets("t:InventoryItem");
        int nextID = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            InventoryItem item = AssetDatabase.LoadAssetAtPath<InventoryItem>(path);
            if (item != null && item.itemData != null)
            {
                item.itemData.itemID = nextID;
                EditorUtility.SetDirty(item);
                Debug.Log($"Assigned itemID {nextID} to {item.name} ({path})");
                nextID++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Auto-assigned itemIDs to {nextID} items.");
    }
}