using System.Collections.Generic;
using UnityEngine;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows;
using Microsoft.VSDiagnostics;

[NativeMemoryProfiler]
[CPUUsageDiagnoser]
public class BiomeObjectSpawnerBenchmark
{
    private BiomeObjectSpawner spawner;
    private Vector2Int center;
    [GlobalSetup]
    public void Setup()
    {
        var gameObject = new GameObject();
        spawner = gameObject.AddComponent<BiomeObjectSpawner>();
        center = new Vector2Int(0, 0);
    // Optionally set up WorldGen.Instance and BiomeData if needed
    }

    [Benchmark]
    public void SpawnObjectsAroundPlayer()
    {
        spawner.SpawnObjectsAroundPlayer(center);
    }
}