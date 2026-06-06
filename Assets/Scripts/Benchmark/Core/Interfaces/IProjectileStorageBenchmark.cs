using System.Diagnostics;
using Benchmark.Core.Enums;
using Benchmark.Data;
using Projectile.Data;

namespace Benchmark.Core.Interfaces
{
    public interface IProjectileStorageBenchmark
    {
        string StructureName { get; }

        bool IsScenarioSupported(BenchmarkScenario scenario);

        void Prepare(BenchmarkConfigData config,  ProjectileDataset dataset);

        int RunScenario(BenchmarkConfigData config, Stopwatch stopwatch);

        void Cleanup();
    }
}