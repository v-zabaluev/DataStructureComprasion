using System.IO;
using Benchmark.Core;
using Benchmark.Core.Enums;
using Benchmark.Data;
using Benchmark.IO;
using NUnit.Framework;
using UnityEngine;

namespace Benchmark.Tests
{
    public class BenchmarkCsvWriterTests
    {
        [Test]
        public void Save_WritesFullConfigSnapshotToCsv()
        {
            string directoryPath = Path.Combine(
                Application.temporaryCachePath,
                "BenchmarkCsvWriterTests");

            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }

            BenchmarkConfigData config = new BenchmarkConfigData
            {
                StructureKind = BenchmarkStructureKind.List,
                Scenario = BenchmarkScenario.GroupBySector,
                ObjectCount = 1234,
                OperationCount = 5678,
                WarmupRuns = 2,
                MeasuredRuns = 4,
                PreallocateCapacity = false,
                PreserveOrderOnRemove = false,
                BaseSeed = 42,
                DeltaTime = 0.02f,
                ProjectileSpeed = 7.5f,
                ProjectileLifeTime = 3.25f,
                SpawnMin = new Vector2(-10f, -20f),
                SpawnMax = new Vector2(110f, 220f),
                EffectCenter = new Vector2(5f, 6f),
                EffectRadius = 17f,
                CellsPerRow = 11,
                AutoCalculateCellSize = false,
                CellSize = 12.5f,
                WaveCount = 6,
                ProjectilesPerWave = 700
            };

            BenchmarkResult result = new BenchmarkResult(
                "List",
                "GroupBySector",
                config.ObjectCount,
                config.OperationCount,
                1.0,
                1.0,
                0.0,
                1.0,
                1.0,
                128,
                0,
                999,
                true,
                "Completed.",
                config);

            BenchmarkCsvWriter writer = new BenchmarkCsvWriter(directoryPath);
            string filePath = writer.Save(result);

            string csv = File.ReadAllText(filePath);

            Assert.That(csv, Does.Contain("WarmupRuns"));
            Assert.That(csv, Does.Contain("MeasuredRuns"));
            Assert.That(csv, Does.Contain("PreallocateCapacity"));
            Assert.That(csv, Does.Contain("PreserveOrderOnRemove"));
            Assert.That(csv, Does.Contain("BaseSeed"));
            Assert.That(csv, Does.Contain("DeltaTime"));
            Assert.That(csv, Does.Contain("ProjectileSpeed"));
            Assert.That(csv, Does.Contain("ProjectileLifeTime"));
            Assert.That(csv, Does.Contain("ObjectsPerCell"));
            Assert.That(csv, Does.Contain(";2;4;False;False;42;0.0200;7.5000;3.2500;"));
            Assert.That(csv, Does.Contain(";-10.0000;-20.0000;110.0000;220.0000;"));
            Assert.That(csv, Does.Contain(";11;False;12.5000;10.1983;6;700;"));
        }

        [Test]
        public void Result_KeepsConfigSnapshot_WhenSourceConfigChanges()
        {
            BenchmarkConfigData config = new BenchmarkConfigData
            {
                ObjectCount = 100,
                OperationCount = 200,
                CellsPerRow = 10,
                AutoCalculateCellSize = false,
                CellSize = 5f,
                BaseSeed = 1
            };

            BenchmarkResult result = new BenchmarkResult(
                "Array",
                "SequentialIteration",
                config.ObjectCount,
                config.OperationCount,
                0.0,
                0.0,
                0.0,
                0.0,
                0.0,
                0,
                0,
                0,
                true,
                "Completed.",
                config);

            config.BaseSeed = 999;
            config.CellSize = 100f;

            Assert.AreEqual(1, result.Config.BaseSeed);
            Assert.AreEqual(5f, result.Config.CellSize);
        }
    }
}
