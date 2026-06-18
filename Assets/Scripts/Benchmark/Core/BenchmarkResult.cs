using Benchmark.Data;

namespace Benchmark.Core
{
    public struct BenchmarkResult
    {
        public readonly string Structure;
        public readonly string Scenario;
        public readonly int Objects;
        public readonly int Operations;

        public readonly double AverageScenarioExecutionTimeMs;
        public readonly double MedianScenarioExecutionTimeMs;
        public readonly double StandardDeviationScenarioExecutionTimeMs;
        public readonly double MinScenarioExecutionTimeMs;
        public readonly double MaxScenarioExecutionTimeMs;

        public readonly long AllocatedBytes;
        public readonly int GCCollections;

        public readonly int Checksum;
        public readonly bool IsSupported;
        public readonly string Message;

        public readonly BenchmarkConfigData Config;

        public readonly float SpawnMinX;
        public readonly float SpawnMinY;
        public readonly float SpawnMaxX;
        public readonly float SpawnMaxY;

        public BenchmarkResult(
            string structure,
            string scenario,
            int objects,
            int operations,
            double averageScenarioExecutionTimeMs,
            double medianScenarioExecutionTimeMs,
            double standardDeviationScenarioExecutionTimeMs,
            double minScenarioExecutionTimeMs,
            double maxScenarioExecutionTimeMs,
            long allocatedBytes,
            int gcCollections,
            int checksum,
            bool isSupported,
            string message,
            BenchmarkConfigData config)
        {
            Structure = structure;
            Scenario = scenario;
            Objects = objects;
            Operations = operations;

            AverageScenarioExecutionTimeMs = averageScenarioExecutionTimeMs;
            MedianScenarioExecutionTimeMs = medianScenarioExecutionTimeMs;
            StandardDeviationScenarioExecutionTimeMs = standardDeviationScenarioExecutionTimeMs;
            MinScenarioExecutionTimeMs = minScenarioExecutionTimeMs;
            MaxScenarioExecutionTimeMs = maxScenarioExecutionTimeMs;

            AllocatedBytes = allocatedBytes;
            GCCollections = gcCollections;

            Checksum = checksum;
            IsSupported = isSupported;
            Message = message;

            Config = config == null ? new BenchmarkConfigData() : config.Clone();

            SpawnMinX = Config.SpawnMin.x;
            SpawnMinY = Config.SpawnMin.y;
            SpawnMaxX = Config.SpawnMax.x;
            SpawnMaxY = Config.SpawnMax.y;
        }
    }
}
