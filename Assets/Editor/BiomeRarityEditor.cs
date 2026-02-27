using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class BiomeRarityEditor : EditorWindow
{
    private List<BiomeData> biomes;
    private Vector2 scrollPos;

    [MenuItem("Tools/Biome Rarity Editor")]
    public static void ShowWindow()
    {
        GetWindow<BiomeRarityEditor>("Biome Rarity Editor");
    }

    private void OnEnable()
    {
        LoadBiomes();
    }

    private void LoadBiomes()
    {
        biomes = new List<BiomeData>();
        string[] guids = AssetDatabase.FindAssets("t:BiomeData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BiomeData biome = AssetDatabase.LoadAssetAtPath<BiomeData>(path);
            if (biome != null)
                biomes.Add(biome);
        }
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Reload Biomes"))
        {
            LoadBiomes();
        }

        if (biomes == null || biomes.Count == 0)
        {
            EditorGUILayout.HelpBox("No BiomeData assets found.", MessageType.Info);
            return;
        }

        int totalRarity = 0;
        foreach (var biome in biomes)
            totalRarity += biome.rarity;

        EditorGUILayout.LabelField("Total Rarity: " + totalRarity, EditorStyles.boldLabel);

        if (totalRarity != 100)
        {
            EditorGUILayout.HelpBox("Total rarity is not 100. Adjust values for true percentages.", MessageType.Warning);
        }

        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var biome in biomes)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(biome.biomeName, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            int newRarity = EditorGUILayout.IntSlider("Rarity", biome.rarity, 0, 100);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(biome, "Change Biome Rarity");
                biome.rarity = newRarity;
                EditorUtility.SetDirty(biome);
            }

            float percent = totalRarity > 0 ? (biome.rarity * 100f / totalRarity) : 0f;
            EditorGUILayout.LabelField("Actual %", percent.ToString("F2") + "%");
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Save All"))
        {
            AssetDatabase.SaveAssets();
        }
    }
}