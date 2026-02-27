using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "BiomeData", menuName = "WorldGen/BiomeData")]
public class BiomeData : ScriptableObject
{
    public string biomeName;
    public TileBase tile;
    public Color previewColor;
    [Range(0, 100)]
    public int rarity = 50; // User sets this

    [Range(0, 100)]
    public int treeRarity = 50; // % chance for tree spawning in this biome

    [Range(0, 100)]
    public int rockRarity = 50; // % chance for rock spawning in this biome

    [HideInInspector]
    public float actualPercentage; // This will be set by the editor

    // Biome-specific prefabs
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;
}