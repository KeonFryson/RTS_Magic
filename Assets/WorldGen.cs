using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class WorldGen : MonoBehaviour
{
    public static WorldGen Instance { get; private set; }
    public event Action<Vector2Int> OnChunkVisibilityChanged;

    [Header("Tilemap Setup")]
    public Tilemap tilemap;
    public Tilemap oceanTilemap;
    public TileBase oceanTile;
    public TileBase deepOceanTile;

    [Header("Biome Setup")]
    public List<BiomeData> biomes;

    [Header("Noise Visualization")]
    public SpriteRenderer noiseRenderer;

    [Header("World Settings")]
    public int width = 50;
    public int height = 50;
    public float waterLevel = 5f;
    public float deepWaterLevel = 3f;

    [Header("Random Seed")]
    public int seed = 0;
    public bool useRandomSeed = true;

    private float offsetX, offsetY, scale = 0.03f;
    private float offsetX2, offsetY2, scale2 = 0.01f;
    private float offsetX3, offsetY3, scale3 = 0.02f;

    public bool showNoise = false;
    private Texture2D noiseTexture;

    private Transform playerTransform;
    private Vector2Int lastPlayerTilePos;
    private HashSet<Vector3Int> drawnTiles = new HashSet<Vector3Int>();

    [Header("UI")]
    [SerializeField] private TMP_Text playerCoordsText;
    [SerializeField] private GameObject loadingPanel;

    [Header("Voronoi Settings")]
    [SerializeField] private int voronoiSiteCount = 100;

    private List<VoronoiSite> voronoiSites;
    public List<VoronoiSite> VoronoiSites => voronoiSites;

    public struct VoronoiSite
    {
        public Vector2 position;
        public BiomeData biome;
    }

    private const int chunkSize = 16;
    private Dictionary<Vector2Int, object[,]> chunkCache = new Dictionary<Vector2Int, object[,]>();
    private Queue<Vector2Int> chunkGenQueue = new Queue<Vector2Int>();
    private HashSet<Vector2Int> chunkGenPending = new HashSet<Vector2Int>();
    private bool isChunkGenRunning = false;
    private bool chunksDirty = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(LoadingPanel(true,true));


        if (useRandomSeed)
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        UnityEngine.Random.InitState(seed);

        offsetX = UnityEngine.Random.Range(0f, 1000f);
        offsetY = UnityEngine.Random.Range(0f, 1000f);
        offsetX2 = UnityEngine.Random.Range(0f, 1000f);
        offsetY2 = UnityEngine.Random.Range(0f, 1000f);
        offsetX3 = UnityEngine.Random.Range(0f, 1000f);
        offsetY3 = UnityEngine.Random.Range(0f, 1000f);

        GenerateVoronoiSites();

        lastPlayerTilePos = GetPlayerTilePosition() + Vector2Int.one * 9999;
        UpdateWorldAroundPlayer();

        StartCoroutine(SpawnPlayerAfterWorldReady());
    }
 
    private IEnumerator SpawnPlayerAfterWorldReady()
    {
        while (isChunkGenRunning)
            yield return null;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            Vector2 spawnPos = FindLandSpawnPosition();
            playerTransform.position = new Vector3(spawnPos.x, spawnPos.y, playerTransform.position.z);
        }


        StartCoroutine(LoadingPanel(false,false));


    }

    private IEnumerator LoadingPanel(bool show,bool IsStartofGame)
    {
        
        while (!IsStartofGame)
        {
            Debug.Log("World generation complete. Spawning player...");
            yield return new WaitForSeconds(0.8f); // Simulate loading time
            IsStartofGame = true;

        }

       
        if (loadingPanel != null)
        {
            Debug.Log(show ? "Showing loading panel..." : "Hiding loading panel...");
            loadingPanel.SetActive(show);
            Player2DMovement.Instance.enabled = !show;  
        }
    }

    private float GetCombinedNoise(int worldX, int worldY)
    {
        float noise1 = Mathf.PerlinNoise(worldX * scale + offsetX, worldY * scale + offsetY);
        float noise2 = Mathf.PerlinNoise(worldX * scale2 + offsetX2, worldY * scale2 + offsetY2);
        float noise3 = Mathf.PerlinNoise(worldX * scale3 + offsetX3, worldY * scale3 + offsetY3);
        return noise1 * 0.5f + noise2 * 0.3f + noise3 * 0.2f;
    }

    private Vector2 FindLandSpawnPosition()
    {
        for (int i = 0; i < 1000; i++)
        {
            float x = UnityEngine.Random.Range(-100f, 100f);
            float y = UnityEngine.Random.Range(-100f, 100f);

            if (IsLand(new Vector2(x, y)))
                return new Vector2(x, y);
        }
        return Vector2.zero;
    }

    void Update()
    {
        Vector2Int playerTilePos = GetPlayerTilePosition();

        if (playerTilePos != lastPlayerTilePos)
        {
            UpdateWorldAroundPlayer();
            OnChunkVisibilityChanged?.Invoke(playerTilePos);
        }

        if (chunksDirty)
        {
            DrawChunksAroundPlayer(lastPlayerTilePos);
            chunksDirty = false;
        }

        if (showNoise && noiseRenderer != null)
        {
            DrawBiomeTexture(GetPlayerTilePosition());
        }
    }

    public Vector2Int GetPlayerTilePosition()
    {
        if (playerTransform == null)
            return Vector2Int.zero;

        Vector3 pos = playerTransform.position;
        return new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
    }

    void UpdateWorldAroundPlayer()
    {
        Vector2Int playerTilePos = GetPlayerTilePosition();
        lastPlayerTilePos = playerTilePos;
        DrawChunksAroundPlayer(playerTilePos);

        if (playerCoordsText != null)
            playerCoordsText.text = $"Player: ({playerTilePos.x}, {playerTilePos.y})";
    }

    void GenerateVoronoiSites()
    {
        voronoiSites = new List<VoronoiSite>();
        float totalRarity = 0f;
        foreach (var biome in biomes)
            totalRarity += biome.rarity;

        for (int i = 0; i < voronoiSiteCount; i++)
        {
            Vector2 pos = new Vector2(
                UnityEngine.Random.Range(-1000f, 1000f),
                UnityEngine.Random.Range(-1000f, 1000f)
            );

            float roll = UnityEngine.Random.value;
            float cumulative = 0f;
            BiomeData selectedBiome = null;

            foreach (var biome in biomes)
            {
                cumulative += biome.rarity / totalRarity;
                if (roll < cumulative)
                {
                    selectedBiome = biome;
                    break;
                }
            }

            if (selectedBiome == null && biomes.Count > 0)
                selectedBiome = biomes[biomes.Count - 1];

            voronoiSites.Add(new VoronoiSite { position = pos, biome = selectedBiome });
        }
    }

    public bool IsLand(Vector2 worldPos)
    {
        return GetCombinedNoise((int)worldPos.x, (int)worldPos.y) >= waterLevel;
    }

    public BiomeData GetVoronoiBiome(Vector2 tilePos)
    {
        float minDist = float.MaxValue;
        BiomeData closest = null;

        foreach (var site in voronoiSites)
        {
            float dist = Vector2.SqrMagnitude(tilePos - site.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = site.biome;
            }
        }
        return closest;
    }
    public bool IsLandTile(Vector2Int tilePos)
    {
        Vector3Int pos = new Vector3Int(tilePos.x, tilePos.y, 0);
        return tilemap.GetTile(pos) != null;
    }

    void DrawChunksAroundPlayer(Vector2Int playerTilePos)
    {
        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt((float)playerTilePos.x / chunkSize),
            Mathf.FloorToInt((float)playerTilePos.y / chunkSize)
        );

        int chunkRadius = Mathf.CeilToInt((float)width / chunkSize / 2);
        var newDrawnTiles = new HashSet<Vector3Int>();

        for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
        {
            for (int dy = -chunkRadius; dy <= chunkRadius; dy++)
            {
                Vector2Int chunkCoord = playerChunk + new Vector2Int(dx, dy);
                GenerateChunkAsync(chunkCoord);

                if (!chunkCache.ContainsKey(chunkCoord))
                    continue;

                object[,] chunkGrid = chunkCache[chunkCoord];
                int worldX0 = chunkCoord.x * chunkSize;
                int worldY0 = chunkCoord.y * chunkSize;

                for (int x = 0; x < chunkSize; x++)
                {
                    for (int y = 0; y < chunkSize; y++)
                    {
                        int worldX = worldX0 + x;
                        int worldY = worldY0 + y;
                        var pos = new Vector3Int(worldX, worldY, 0);
                        newDrawnTiles.Add(pos);

                        TileBase landTile = null;
                        TileBase oceanTileToSet = null;

                        if (chunkGrid[x, y] is TileBase waterTile)
                            oceanTileToSet = waterTile;
                        else if (chunkGrid[x, y] is BiomeData biomeData)
                            landTile = biomeData.tile;

                        tilemap.SetTile(pos, landTile);
                        oceanTilemap.SetTile(pos, oceanTileToSet);
                    }
                }
            }
        }

        foreach (var oldPos in drawnTiles)
        {
            if (!newDrawnTiles.Contains(oldPos))
            {
                tilemap.SetTile(oldPos, null);
                oceanTilemap.SetTile(oldPos, null);
            }
        }

        drawnTiles = newDrawnTiles;
    }

    void GenerateChunkAsync(Vector2Int chunkCoord)
    {
        if (chunkCache.ContainsKey(chunkCoord) || chunkGenPending.Contains(chunkCoord))
            return;

        chunkGenPending.Add(chunkCoord);
        chunkGenQueue.Enqueue(chunkCoord);
        if (!isChunkGenRunning)
            StartCoroutine(ProcessChunkGenQueue());
    }

    IEnumerator ProcessChunkGenQueue()
    {
        isChunkGenRunning = true;

        while (chunkGenQueue.Count > 0)
        {
            Vector2Int chunkCoord = chunkGenQueue.Dequeue();
            object[,] chunkGrid = null;

            var task = Task.Run(() =>
            {
                chunkGrid = GenerateChunkData(chunkCoord);
            });

            while (!task.IsCompleted)
                yield return null;

            chunkCache[chunkCoord] = chunkGrid;
            chunkGenPending.Remove(chunkCoord);
            chunksDirty = true;
        }

        isChunkGenRunning = false;
    }

    private object[,] GenerateChunkData(Vector2Int chunkCoord)
    {
        object[,] chunkGrid = new object[chunkSize, chunkSize];
        int worldX0 = chunkCoord.x * chunkSize;
        int worldY0 = chunkCoord.y * chunkSize;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                int worldX = worldX0 + x;
                int worldY = worldY0 + y;
                float noise = GetCombinedNoise(worldX, worldY);

                if (noise < deepWaterLevel)
                    chunkGrid[x, y] = deepOceanTile;
                else if (noise < waterLevel)
                    chunkGrid[x, y] = oceanTile;
                else
                    chunkGrid[x, y] = GetVoronoiBiome(new Vector2(worldX, worldY));
            }
        }

        return chunkGrid;
    }


    void DrawNoiseTexture(Vector2Int center)
    {
        if (noiseTexture == null || noiseTexture.width != width || noiseTexture.height != height)
        {
            noiseTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            noiseTexture.filterMode = FilterMode.Point;
        }

        int xOffset = width / 2;
        int yOffset = height / 2;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int worldX = center.x + x - xOffset;
                int worldY = center.y + y - yOffset;

                float nx1 = worldX * scale + offsetX;
                float ny1 = worldY * scale + offsetY;
                float noise1 = Mathf.PerlinNoise(nx1, ny1);

                float nx2 = worldX * scale2 + offsetX2;
                float ny2 = worldY * scale2 + offsetY2;
                float noise2 = Mathf.PerlinNoise(nx2, ny2);

                float nx3 = worldX * scale3 + offsetX3;
                float ny3 = worldY * scale3 + offsetY3;
                float noise3 = Mathf.PerlinNoise(nx3, ny3);

                float noise = (noise1 + noise2 + noise3) / 3f;
                Color c = new Color(noise, noise, noise, 1f);
                noiseTexture.SetPixel(x, y, c);
            }
        }
        noiseTexture.Apply();

        if (noiseRenderer != null)
        {
            noiseRenderer.sprite = Sprite.Create(noiseTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
        }
    }

    void UpdateVisualization()
    {
        if (noiseRenderer != null)
        {
            noiseRenderer.gameObject.SetActive(showNoise);
        }
        if (tilemap != null)
        {
            tilemap.gameObject.SetActive(!showNoise);
        }
        if (playerCoordsText != null)
            playerCoordsText.gameObject.SetActive(!showNoise);
    }

    void DrawBiomeTexture(Vector2Int center)
    {
        if (noiseTexture == null || noiseTexture.width != width || noiseTexture.height != height)
        {
            noiseTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            noiseTexture.filterMode = FilterMode.Point;
        }

        int xOffset = width / 2;
        int yOffset = height / 2;

        // Cache nearest site for each pixel
        int[,] siteIndices = new int[width, height];

        // First pass: assign site indices
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int worldX = center.x + x - xOffset;
                int worldY = center.y + y - yOffset;

                float nx1 = worldX * scale + offsetX;
                float ny1 = worldY * scale + offsetY;
                float noise1 = Mathf.PerlinNoise(nx1, ny1);

                float nx2 = worldX * scale2 + offsetX2;
                float ny2 = worldY * scale2 + offsetY2;
                float noise2 = Mathf.PerlinNoise(nx2, ny2);

                float nx3 = worldX * scale3 + offsetX3;
                float ny3 = worldY * scale3 + offsetY3;
                float noise3 = Mathf.PerlinNoise(nx3, ny3);

                float noise = (noise1 * 0.5f + noise2 * 0.3f + noise3 * 0.2f);

                if (noise < deepWaterLevel || noise < waterLevel)
                {
                    siteIndices[x, y] = -1; // Water, not a Voronoi cell
                }
                else
                {
                    Vector2 tilePos = new Vector2(worldX, worldY);
                    int nearestSite = GetNearestVoronoiSiteIndex(tilePos);
                    siteIndices[x, y] = nearestSite;
                }
            }
        }

        // Second pass: draw with borders
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color c;
                if (siteIndices[x, y] == -1)
                {
                    // Water coloring
                    int worldX = center.x + x - xOffset;
                    int worldY = center.y + y - yOffset;
                    float nx1 = worldX * scale + offsetX;
                    float ny1 = worldY * scale + offsetY;
                    float noise1 = Mathf.PerlinNoise(nx1, ny1);
                    float nx2 = worldX * scale2 + offsetX2;
                    float ny2 = worldY * scale2 + offsetY2;
                    float noise2 = Mathf.PerlinNoise(nx2, ny2);
                    float nx3 = worldX * scale3 + offsetX3;
                    float ny3 = worldY * scale3 + offsetY3;
                    float noise3 = Mathf.PerlinNoise(nx3, ny3);
                    float noise = (noise1 * 0.5f + noise2 * 0.3f + noise3 * 0.2f);
                    c = (noise < deepWaterLevel) ? Color.blue : Color.cyan;
                }
                else
                {
                    // Check 4-neighbors for border
                    bool isBorder = false;
                    int currentSite = siteIndices[x, y];
                    for (int dx = -1; dx <= 1 && !isBorder; dx++)
                    {
                        for (int dy = -1; dy <= 1 && !isBorder; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx, ny = y + dy;
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                if (siteIndices[nx, ny] != currentSite && siteIndices[nx, ny] != -1)
                                    isBorder = true;
                            }
                        }
                    }
                    if (isBorder)
                        c = Color.black; // Border color
                    else
                    {
                        var biome = voronoiSites[currentSite].biome;
                        c = biome != null ? biome.previewColor : Color.magenta;
                    }
                }
                noiseTexture.SetPixel(x, y, c);
            }
        }
        noiseTexture.Apply();

        if (noiseRenderer != null)
        {
            noiseRenderer.sprite = Sprite.Create(noiseTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1f);
        }
    }

    // Helper to get the nearest Voronoi site index
    int GetNearestVoronoiSiteIndex(Vector2 tilePos)
    {
        float minDist = float.MaxValue;
        int closestIndex = 0;
        for (int i = 0; i < voronoiSites.Count; i++)
        {
            float dist = Vector2.SqrMagnitude(tilePos - voronoiSites[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

}