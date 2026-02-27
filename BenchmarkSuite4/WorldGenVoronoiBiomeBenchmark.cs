using System;
using BenchmarkDotNet.Attributes;
using UnityEngine;
using Microsoft.VSDiagnostics;

[CPUUsageDiagnoser]
public class WorldGenVoronoiBiomeBenchmark
{
    private WorldGen worldGen;
    private Vector2[] testPositions;
    [GlobalSetup]
    public void Setup()
    {
        // Create a WorldGen instance (mock or real, depending on test context)
        worldGen = WorldGen.Instance;
        if (worldGen == null)
        {
            var go = new GameObject("WorldGen");
            worldGen = go.AddComponent<WorldGen>();
            worldGen.biomes = new System.Collections.Generic.List<BiomeData>();
            worldGen.VoronoiSites.Clear();
            worldGen.GenerateVoronoiSites();
        }

        // Generate test positions in a grid
        int n = 100;
        testPositions = new Vector2[n * n];
        int idx = 0;
        for (int x = -50; x < 50; x += 1)
            for (int y = -50; y < 50; y += 1)
                testPositions[idx++] = new Vector2(x, y);
    }

    [Benchmark]
    public void GetVoronoiBiome_Grid()
    {
        for (int i = 0; i < testPositions.Length; i++)
        {
            var biome = worldGen.GetVoronoiBiome(testPositions[i]);
        }
    }
}