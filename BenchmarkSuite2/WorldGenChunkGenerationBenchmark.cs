using System;
using BenchmarkDotNet.Attributes;
using UnityEngine;
using Microsoft.VSDiagnostics;

[Microsoft.VisualStudio.Diagnostics.BenchmarkRunner.Windows.Configs.VSDiagnosticsExporter]
[CPUUsageDiagnoser]
public class WorldGenChunkGenerationBenchmark
{
    private WorldGen worldGen;
    private Vector2Int chunkCoord;
    [GlobalSetup]
    public void Setup()
    {
        // Create a new GameObject and attach WorldGen
        var go = new GameObject("WorldGenBenchmark");
        worldGen = go.AddComponent<WorldGen>();
        worldGen.width = 50;
        worldGen.height = 50;
        worldGen.useRandomSeed = false;
        worldGen.seed = 12345;
        worldGen.biomes = new System.Collections.Generic.List<BiomeData>(); // Add dummy biomes if needed
        worldGen.oceanTile = ScriptableObject.CreateInstance<TileBase>();
        worldGen.deepOceanTile = ScriptableObject.CreateInstance<TileBase>();
        worldGen.Start();
        chunkCoord = new Vector2Int(0, 0);
    }

    [Benchmark]
    public void GenerateChunkDataBenchmark()
    {
        worldGen.GenerateChunkData(chunkCoord);
    }
}