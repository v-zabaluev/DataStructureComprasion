using System.Collections.Generic;
using Benchmark.Core.Enums;
using UnityEngine;

namespace Benchmark.Data
{
    [CreateAssetMenu(
        fileName = "BenchmarkBatchConfigSet",
        menuName = "Benchmark/Batch Config Set")]
    public class BenchmarkBatchConfigSet : ScriptableObject
    {
        [SerializeField] private List<BenchmarkConfigData> _configs =
            new List<BenchmarkConfigData>();

        public IReadOnlyList<BenchmarkConfigData> Configs => _configs;

        public int Count
        {
            get
            {
                if (_configs == null)
                {
                    return 0;
                }

                return _configs.Count;
            }
        }

        public BenchmarkConfigData GetConfig(int index)
        {
            return _configs[index];
        }

        public void FillRecommendedMobileResearchConfigs()
        {
            _configs = new List<BenchmarkConfigData>();

            const int accessOperationCount = 10000;

            _configs.Add(CreateConfig(
                objectCount: 100,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: true,
                preserveOrderOnRemove: false,
                baseSeed: 101,
                waveCount: 5,
                projectilesPerWave: 20,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));

            _configs.Add(CreateConfig(
                objectCount: 500,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: true,
                preserveOrderOnRemove: false,
                baseSeed: 102,
                waveCount: 5,
                projectilesPerWave: 100,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));

            _configs.Add(CreateConfig(
                objectCount: 1000,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: true,
                preserveOrderOnRemove: false,
                baseSeed: 103,
                waveCount: 5,
                projectilesPerWave: 200,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));

            _configs.Add(CreateConfig(
                objectCount: 2500,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: true,
                preserveOrderOnRemove: false,
                baseSeed: 104,
                waveCount: 5,
                projectilesPerWave: 500,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));

            _configs.Add(CreateConfig(
                objectCount: 5000,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: true,
                preserveOrderOnRemove: false,
                baseSeed: 105,
                waveCount: 5,
                projectilesPerWave: 1000,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));

            _configs.Add(CreateConfig(
                objectCount: 10000,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: true,
                preserveOrderOnRemove: false,
                baseSeed: 106,
                waveCount: 5,
                projectilesPerWave: 2000,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));

            _configs.Add(CreateConfig(
                objectCount: 25000,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: true,
                preserveOrderOnRemove: false,
                baseSeed: 107,
                waveCount: 5,
                projectilesPerWave: 5000,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));

            _configs.Add(CreateConfig(
                objectCount: 50000,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: true,
                preserveOrderOnRemove: false,
                baseSeed: 108,
                waveCount: 5,
                projectilesPerWave: 10000,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));

            _configs.Add(CreateConfig(
                objectCount: 10000,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: true,
                preserveOrderOnRemove: true,
                baseSeed: 201,
                waveCount: 5,
                projectilesPerWave: 2000,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));

            _configs.Add(CreateConfig(
                objectCount: 2500,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: false,
                preserveOrderOnRemove: false,
                baseSeed: 301,
                waveCount: 5,
                projectilesPerWave: 500,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));

            _configs.Add(CreateConfig(
                objectCount: 10000,
                operationCount: accessOperationCount,
                warmupRuns: 3,
                measuredRuns: 10,
                preallocateCapacity: false,
                preserveOrderOnRemove: false,
                baseSeed: 302,
                waveCount: 5,
                projectilesPerWave: 2000,
                projectileSpeed: 5f,
                projectileLifeTime: 5f,
                effectRadius: 15f));
        }

        private BenchmarkConfigData CreateConfig(
            int objectCount,
            int operationCount,
            int warmupRuns,
            int measuredRuns,
            bool preallocateCapacity,
            bool preserveOrderOnRemove,
            int baseSeed,
            int waveCount,
            int projectilesPerWave,
            float projectileSpeed,
            float projectileLifeTime,
            float effectRadius)
        {
            BenchmarkConfigData config = new BenchmarkConfigData();

            config.StructureKind = BenchmarkStructureKind.Array;
            config.Scenario = BenchmarkScenario.SequentialIteration;
            config.ObjectCount = objectCount;
            config.OperationCount = operationCount;
            config.WarmupRuns = warmupRuns;
            config.MeasuredRuns = measuredRuns;
            config.PreallocateCapacity = preallocateCapacity;
            config.PreserveOrderOnRemove = preserveOrderOnRemove;
            config.BaseSeed = baseSeed;
            config.DeltaTime = 0.016f;
            config.ProjectileSpeed = projectileSpeed;
            config.ProjectileLifeTime = projectileLifeTime;
            config.SpawnMin = new Vector2(0f, 0f);
            config.SpawnMax = new Vector2(100f, 100f);
            config.EffectCenter = new Vector2(50f, 50f);
            config.EffectRadius = effectRadius;
            config.WaveCount = waveCount;
            config.ProjectilesPerWave = projectilesPerWave;

            config.Normalize();

            return config;
        }
    }
}