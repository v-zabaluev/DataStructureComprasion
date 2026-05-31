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

        public int RunScenario(BenchmarkConfigData config)
        {
            return 123;
        }

        public void Cleanup()
        {
        }
    }
}