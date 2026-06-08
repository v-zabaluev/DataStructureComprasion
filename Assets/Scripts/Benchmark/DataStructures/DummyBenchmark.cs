using System.Diagnostics;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile.Data;

namespace Benchmark.DataStructures
{
    public class DummyBenchmark : IProjectileStorageBenchmark
    {
        public string StructureName => "Dummy";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return true;
        }

        public void Prepare(
            BenchmarkConfigData config,
            ProjectileDataset dataset)
        {
        }

        public int RunScenario(BenchmarkConfigData config, Stopwatch stopwatch)
        {
            global::Benchmark.Core.BenchmarkTimer.Restart(stopwatch);

            int checksum = 123;

            global::Benchmark.Core.BenchmarkTimer.Stop(stopwatch);
            return checksum;
        }

        public void Cleanup()
        {
        }
    }
}