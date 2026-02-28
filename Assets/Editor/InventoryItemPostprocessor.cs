using UnityEngine;
using UnityEditor;
using System.Linq;

public class InventoryItemPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            InventoryItem item = AssetDatabase.LoadAssetAtPath<InventoryItem>(assetPath);
            if (item != null)
            {
                // Find the ItemDatabase asset in the project
                string[] guids = AssetDatabase.FindAssets("t:ItemDatabase");
                if (guids.Length > 0)
                {
                    string dbPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    ItemDatabase db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(dbPath);
                    if (db != null)
                    {
                        // Add the item if not already present
                        if (db.allItems == null)
                            db.allItems = new InventoryItem[0];

                        if (!db.allItems.Contains(item))
                        {
                            var newList = db.allItems.ToList();
                            newList.Add(item);
                            db.allItems = newList.ToArray();
                            EditorUtility.SetDirty(db);
                            Debug.Log($"[ItemDatabase] Added new InventoryItem '{item.name}' to ItemDatabase.");
                        }
                    }
                }
            }
        }
    }
}