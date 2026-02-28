using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    // --- NEW INPUT SYSTEM FIELD ---
    private InputAction mouseClickAction;
    private Dictionary<Vector2Int, GameObject> spawnedForests = new Dictionary<Vector2Int, GameObject>();

 
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        //mouseClickAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        //mouseClickAction.performed += ctx => PrintTileUnderMouse();
        //mouseClickAction.Enable();
    }

    private void OnDisable()
    {
        mouseClickAction?.Disable();
    }

    private void Start()
    {
        StartCoroutine(LoadingPanel(true, true));

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

        if (showNoise)
        {
            UpdateVisualization();
            if (noiseRenderer != null)
                DrawBiomeTexture(Vector2Int.zero);
            StartCoroutine(LoadingPanel(false, false));
            return;
        }

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

        StartCoroutine(LoadingPanel(false, false));
    }

    private IEnumerator LoadingPanel(bool show, bool IsStartofGame)
    {
        while (!IsStartofGame)
        {
            Debug.Log("World generation complete. Spawning player...");
            yield return new WaitForSeconds(0.8f);
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
        int maxAttemptsPerRange = 1000;
        int range = 10;
        int rangeStep = 30;

        while (true)
        {
            for (int i = 0; i < maxAttemptsPerRange; i++)
            {
                float x = UnityEngine.Random.Range(-range, range + 1);
                float y = UnityEngine.Random.Range(-range, range + 1);

                Vector2Int tilePos = new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
                if (IsLandTile(tilePos))
                    return new Vector2(x, y);
            }
            range += rangeStep;
        }
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
        int totalRarity = 0;
        foreach (var biome in biomes)
            totalRarity += biome.rarity;

        for (int i = 0; i < voronoiSiteCount; i++)
        {
            Vector2 pos = new Vector2(
                UnityEngine.Random.Range(-1000f, 1000f),
                UnityEngine.Random.Range(-1000f, 1000f)
            );

            int roll = UnityEngine.Random.Range(0, totalRarity);
            int cumulative = 0;
            BiomeData selectedBiome = null;

            foreach (var biome in biomes)
            {
                cumulative += biome.rarity;
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

    public bool IsLandTile(Vector2Int tilePos)
    {
        Vector3Int pos = new Vector3Int(tilePos.x, tilePos.y, 0);
        TileBase landTile = tilemap.GetTile(pos);
        TileBase waterTile = oceanTilemap != null ? oceanTilemap.GetTile(pos) : null;

        // Land tile exists and there is no water tile at this position
        return landTile != null && waterTile == null;
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

                SpawnForests();
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

        // Cleanup forests that are no longer visible
        CleanupForests(newDrawnTiles);

        drawnTiles = newDrawnTiles;
    }

    // Forest spawning now happens after all chunks are generated
    private void SpawnForests()
    {
        

        foreach (var pos in drawnTiles)
        {
            Vector2Int tilePos = new Vector2Int(pos.x, pos.y);
            if (SaveManager.IsResourceNodeDestroyed(tilePos))
                continue;

            if (spawnedForests.ContainsKey(tilePos))
                continue;

            TileBase landTile = tilemap.GetTile(pos);
            TileBase waterTile = oceanTilemap != null ? oceanTilemap.GetTile(pos) : null;
            if (landTile == null || waterTile != null)
                continue;

            BiomeData biomeData = null;
            foreach (var biome in biomes)
            {
                if (biome.tile == landTile)
                {
                    biomeData = biome;
                    break;
                }
            }
            if (biomeData == null || biomeData.treePrefabs == null || biomeData.treePrefabs.Length == 0 || biomeData.treeRarity <= 0)
                continue;

            int hash = tilePos.x * 73856093 ^ tilePos.y * 19349663 ^ seed;
            UnityEngine.Random.InitState(hash);

            if (UnityEngine.Random.Range(0, 100) < biomeData.treeRarity)
            {
                GameObject prefab = biomeData.treePrefabs[UnityEngine.Random.Range(0, biomeData.treePrefabs.Length)];
                // Use tilemap to get the exact center of the tile in world space
                Vector3 spawnPos = tilemap.GetCellCenterWorld(pos);

                if (waterTile != null)
                {
                    Debug.LogWarning($"Attempting to spawn tree at ({pos.x},{pos.y}) on ocean tile: {waterTile.name}");
                }

                GameObject tree = Instantiate(prefab, spawnPos, Quaternion.identity, this.transform);
                spawnedForests[tilePos] = tree;
            }
        }
    }

    

    private void CleanupForests(HashSet<Vector3Int> newDrawnTiles)
    {
        var toRemove = new List<Vector2Int>();
        foreach (var kvp in spawnedForests)
        {
            Vector2Int tilePos = kvp.Key;
            if (!newDrawnTiles.Contains(new Vector3Int(tilePos.x, tilePos.y, 0)))
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
                toRemove.Add(tilePos);
            }
        }
        foreach (var pos in toRemove)
            spawnedForests.Remove(pos);
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
        int chunksPerFrame = 4;

        while (chunkGenQueue.Count > 0)
        {
            int processed = 0;
            while (chunkGenQueue.Count > 0 && processed < chunksPerFrame)
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
                processed++;
            }
            yield return null;
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

        int[,] siteIndices = new int[width, height];

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
                    siteIndices[x, y] = -1;
                }
                else
                {
                    Vector2 tilePos = new Vector2(worldX, worldY);
                    int nearestSite = GetNearestVoronoiSiteIndex(tilePos);
                    siteIndices[x, y] = nearestSite;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color c;
                if (siteIndices[x, y] == -1)
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
                    c = (noise < deepWaterLevel) ? Color.blue : Color.cyan;
                }
                else
                {
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
                        c = Color.black;
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

    // --- PRINT TILE UNDER MOUSE USING NEW INPUT SYSTEM ---
    private void PrintTileUnderMouse()
    {
        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -Camera.main.transform.position.z));
        Vector3Int cellPos = tilemap.WorldToCell(mouseWorld);

        TileBase landTile = tilemap.GetTile(cellPos);
        TileBase waterTile = oceanTilemap != null ? oceanTilemap.GetTile(cellPos) : null;

        string info = $"Mouse at world {mouseWorld}, cell {cellPos}: ";
        if (landTile != null)
        {
            string biomeName = "Unknown";
            foreach (var biome in biomes)
            {
                if (biome.tile == landTile)
                {
                    biomeName = biome.name;
                    break;
                }
            }
            info += $"LAND ({biomeName})";
        }
        else if (waterTile != null)
        {
            if (waterTile == oceanTile)
                info += "OCEAN";
            else if (waterTile == deepOceanTile)
                info += "DEEP OCEAN";
            else
                info += "WATER (unknown type)";
        }
        else
        {
            info += "NO TILE";
        }

        Debug.Log(info);
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
         

        //if (tilemap == null)
        //    return;

        //Gizmos.color = Color.green;
        //int minX = -width / 2;
        //int maxX = width / 2;
        //int minY = -height / 2;
        //int maxY = height / 2;

        //for (int x = minX; x < maxX; x++)
        //{
        //    for (int y = minY; y < maxY; y++)
        //    {
        //        Vector2Int tilePos = new Vector2Int(x, y);
        //        if (IsLandTile(tilePos))
        //        {
        //            Vector3Int cellPos = new Vector3Int(x, y, 0);
        //            Vector3 worldPos = tilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);
        //            Gizmos.DrawWireCube(worldPos, Vector3.one * 0.95f);
        //        }
        //    }
        //}

        //// Draw a red box for every spawned tree
        //Gizmos.color = Color.red;
        //foreach (var kvp in spawnedForests)
        //{
        //    Vector2Int tilePos = kvp.Key;
        //    Vector3Int cellPos = new Vector3Int(tilePos.x, tilePos.y, 0);
        //    Vector3 worldPos = tilemap.CellToWorld(cellPos) + new Vector3(0.5f, 0.5f, 0f);
        //    Gizmos.DrawWireCube(worldPos, Vector3.one * 0.7f);
        //}
    }
#endif

}