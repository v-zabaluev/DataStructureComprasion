using Benchmark.Data;
using Projectile.Data;
using UnityEngine;
using Random = System.Random;

namespace Projectile
{
    public class ProjectileDatasetGenerator
    {
        private readonly ProjectileDataFactory _factory;

        public ProjectileDatasetGenerator()
        {
            _factory = new ProjectileDataFactory();
        }

        public ProjectileDataset Generate(
            BenchmarkConfigData config,
            int iterationIndex)
        {
            config.Normalize();

            int seed = config.BaseSeed + iterationIndex;
            Random random = new Random(seed);

            ProjectileData[] projectiles = new ProjectileData[config.ObjectCount];

            for (int i = 0; i < config.ObjectCount; i++)
            {
                ProjectileCreationData creationData = CreateCreationData(
                    i,
                    random,
                    config);

                projectiles[i] = _factory.Create(creationData);
            }

            return new ProjectileDataset(
                projectiles,
                seed,
                iterationIndex);
        }

        private ProjectileCreationData CreateCreationData(
            int id,
            Random random,
            BenchmarkConfigData config)
        {
            Vector2 position = CreatePosition(random, config);
            Vector2 direction = CreateDirection(random);

            return new ProjectileCreationData(
                id,
                position,
                direction,
                config.ProjectileSpeed,
                config.ProjectileLifeTime);
        }

        private Vector2 CreatePosition(Random random, BenchmarkConfigData config)
        {
            float x = NextFloat(
                random,
                config.SpawnMin.x,
                config.SpawnMax.x);

            float y = NextFloat(
                random,
                config.SpawnMin.y,
                config.SpawnMax.y);

            return new Vector2(x, y);
        }

        private Vector2 CreateDirection(Random random)
        {
            float angle = NextFloat(random, 0f, 360f) * Mathf.Deg2Rad;

            Vector2 direction = new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle));

            return direction.normalized;
        }

        private float NextFloat(Random random, float minInclusive, float maxInclusive)
        {
            double value = random.NextDouble();
            return minInclusive + (float) value * (maxInclusive - minInclusive);
        }
    }
}
