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

            int[] objectCounts =
            {
                100,
                500,
                1000,
                2500,
                5000,
                10000,
                25000,
                50000
            };

            AddConfigGroup(
                objectCounts,
                accessOperationCount,
                preallocateCapacity: true,
                preserveOrderOnRemove: false,
                baseSeedStart: 101);

            int[] objectCounts2 =
            {
                100,
                500,
                1000,
                2500,
                5000,
            };

            AddConfigGroup(
                objectCounts2,
                accessOperationCount,
                preallocateCapacity: false,
                preserveOrderOnRemove: false,
                baseSeedStart: 201);

            AddConfigGroup(
                objectCounts2,
                accessOperationCount,
                preallocateCapacity: true,
                preserveOrderOnRemove: true,
                baseSeedStart: 301);
        }

        private void AddConfigGroup(
            int[] objectCounts,
            int operationCount,
            bool preallocateCapacity,
            bool preserveOrderOnRemove,
            int baseSeedStart)
        {
            for (int i = 0; i < objectCounts.Length; i++)
            {
                _configs.Add(CreateConfig(
                    objectCount: objectCounts[i],
                    operationCount: operationCount,
                    warmupRuns: 3,
                    measuredRuns: 10,
                    preallocateCapacity: preallocateCapacity,
                    preserveOrderOnRemove: preserveOrderOnRemove,
                    baseSeed: baseSeedStart + i,
                    projectileSpeed: 5f,
                    projectileLifeTime: 5f));
            }
        }

        private BenchmarkConfigData CreateConfig(
            int objectCount,
            int operationCount,
            int warmupRuns,
            int measuredRuns,
            bool preallocateCapacity,
            bool preserveOrderOnRemove,
            int baseSeed,
            float projectileSpeed,
            float projectileLifeTime)
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

            config.Normalize();

            return config;
        }
    }
}