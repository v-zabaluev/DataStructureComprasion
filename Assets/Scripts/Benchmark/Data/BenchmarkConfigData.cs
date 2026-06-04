using System;
using Benchmark.Core.Enums;
using UnityEngine;

namespace Benchmark.Data
{
    [Serializable]
    public class BenchmarkConfigData
    {
        [Header("Target")]

        public BenchmarkStructureKind StructureKind = BenchmarkStructureKind.Array;

        public BenchmarkScenario Scenario = BenchmarkScenario.SequentialIteration;

        [Header("Load")]

        [Min(1)] public int ObjectCount = 10000;

        [Min(1)] public int OperationCount = 10000;
        [Min(1)] public int WarmupRuns = 3;
        [Min(1)] public int MeasuredRuns = 10;

        [Header("Rules")]

        public bool PreallocateCapacity = true;

        public bool PreserveOrderOnRemove = true;

        [Header("Random")]

        public int BaseSeed = 1;

        [Header("Projectile")]

        public float DeltaTime = 0.016f;

        public float ProjectileSpeed = 5f;
        public float ProjectileLifeTime = 5f;

        [Header("World")]

        public Vector2 SpawnMin = new Vector2(0f, 0f);

        public Vector2 SpawnMax = new Vector2(100f, 100f);

        [Header("Effect Area")]

        public Vector2 EffectCenter = new Vector2(50f, 50f);

        public float EffectRadius = 15f;

        [Header("Wave")]

        [Min(1)] public int WaveCount = 5;

        [Min(1)] public int ProjectilesPerWave = 1000;

        public BenchmarkConfigData Clone()
        {
            BenchmarkConfigData clone = new BenchmarkConfigData
            {
                StructureKind = StructureKind,
                Scenario = Scenario,
                ObjectCount = ObjectCount,
                OperationCount = OperationCount,
                WarmupRuns = WarmupRuns,
                MeasuredRuns = MeasuredRuns,
                PreallocateCapacity = PreallocateCapacity,
                PreserveOrderOnRemove = PreserveOrderOnRemove,
                BaseSeed = BaseSeed,
                DeltaTime = DeltaTime,
                ProjectileSpeed = ProjectileSpeed,
                ProjectileLifeTime = ProjectileLifeTime,
                SpawnMin = SpawnMin,
                SpawnMax = SpawnMax,
                EffectCenter = EffectCenter,
                EffectRadius = EffectRadius,
                WaveCount = WaveCount,
                ProjectilesPerWave = ProjectilesPerWave
            };

            clone.Normalize();

            return clone;
        }

        public void Normalize()
        {
            if (ObjectCount < 1)
            {
                ObjectCount = 1;
            }

            if (OperationCount < 1)
            {
                OperationCount = 1;
            }

            if (WarmupRuns < 1)
            {
                WarmupRuns = 1;
            }

            if (MeasuredRuns < 1)
            {
                MeasuredRuns = 1;
            }

            if (WaveCount < 1)
            {
                WaveCount = 1;
            }

            if (ProjectilesPerWave < 1)
            {
                ProjectilesPerWave = 1;
            }

            if (SpawnMax.x < SpawnMin.x)
            {
                float temp = SpawnMin.x;
                SpawnMin.x = SpawnMax.x;
                SpawnMax.x = temp;
            }

            if (SpawnMax.y < SpawnMin.y)
            {
                float temp = SpawnMin.y;
                SpawnMin.y = SpawnMax.y;
                SpawnMax.y = temp;
            }

            if (EffectRadius < 0f)
            {
                EffectRadius = 0f;
            }
        }
    }
}