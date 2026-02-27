using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BiomeData))]
public class BiomeDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BiomeData current = (BiomeData)target;

        // Find all BiomeData assets in the project
        string[] guids = AssetDatabase.FindAssets("t:BiomeData");
        int totalRarity = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BiomeData biome = AssetDatabase.LoadAssetAtPath<BiomeData>(path);
            if (biome != null)
                totalRarity += biome.rarity;
        }

        float percent = totalRarity > 0 ? (current.rarity * 100f / totalRarity) : 0f;
        current.actualPercentage = percent;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actual Distribution %", percent.ToString("F2") + "%", EditorStyles.boldLabel);

        if (totalRarity != 100)
        {
            EditorGUILayout.HelpBox("Total rarity of all biomes is " + totalRarity + ". Adjust so it sums to 100 for true percentages.", MessageType.Warning);
        }
    }
}