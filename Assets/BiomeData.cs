using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "BiomeData", menuName = "WorldGen/BiomeData")]
public class BiomeData : ScriptableObject
{
    public string biomeName;
    public TileBase tile;
    public Color previewColor;
    [Range(0f, 1f)]
    public float rarity = 0.5f;

    // Biome-specific prefabs
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;
}