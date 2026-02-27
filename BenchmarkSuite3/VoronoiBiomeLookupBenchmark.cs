using System;
using BenchmarkDotNet.Attributes;
using UnityEngine;
using Microsoft.VSDiagnostics;

[CPUUsageDiagnoser]
public class VoronoiBiomeLookupBenchmark
{
    private WorldGen worldGen;
    private Vector2[] testPositions;
    [GlobalSetup]
    public void Setup()
    {
        worldGen = WorldGen.Instance;
        if (worldGen == null)
        {
            var go = new GameObject("WorldGen");
            worldGen = go.AddComponent<WorldGen>();
            worldGen.biomes = new System.Collections.Generic.List<BiomeData>();
            worldGen.voronoiSites = new System.Collections.Generic.List<WorldGen.VoronoiSite>();
            worldGen.GenerateVoronoiSites();
        }

        testPositions = new Vector2[1000];
        var rand = new System.Random(42);
        for (int i = 0; i < testPositions.Length; i++)
        {
            float x = (float)(rand.NextDouble() * 2000 - 1000);
            float y = (float)(rand.NextDouble() * 2000 - 1000);
            testPositions[i] = new Vector2(x, y);
        }
    }

    [Benchmark]
    public void LookupVoronoiBiome()
    {
        for (int i = 0; i < testPositions.Length; i++)
        {
            var biome = worldGen.GetVoronoiBiome(testPositions[i]);
        }
    }
}