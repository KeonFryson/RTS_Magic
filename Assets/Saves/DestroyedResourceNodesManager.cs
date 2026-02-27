using System.Collections.Generic;
using UnityEngine;

public static class DestroyedResourceNodesManager
{
    private static HashSet<Vector2Int> destroyedNodes = new HashSet<Vector2Int>();
    private const string SaveKey = "DestroyedResourceNodes";

    static DestroyedResourceNodesManager()
    {
        Load();
    }

    public static void MarkDestroyed(Vector2Int tilePos)
    {
        destroyedNodes.Add(tilePos);
        Save();
    }

    public static bool IsDestroyed(Vector2Int tilePos)
    {
        return destroyedNodes.Contains(tilePos);
    }

    public static void Save()
    {
        // Serialize as comma-separated: "x1_y1,x2_y2,..."
        var list = new List<string>();
        foreach (var pos in destroyedNodes)
            list.Add($"{pos.x}_{pos.y}");
        PlayerPrefs.SetString(SaveKey, string.Join(",", list));
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        destroyedNodes.Clear();
        var data = PlayerPrefs.GetString(SaveKey, "");
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