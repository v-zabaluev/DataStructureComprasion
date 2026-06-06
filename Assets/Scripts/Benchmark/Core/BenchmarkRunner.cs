using System;
using System.Diagnostics;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile;
using Projectile.Data;

namespace Benchmark.Core
{
    public class BenchmarkRunner
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly ProjectileDatasetGenerator _datasetGenerator = new ProjectileDatasetGenerator();

        public BenchmarkResult Run(
            IProjectileStorageBenchmark benchmark,
            BenchmarkConfigData config,
            Action<BenchmarkProgress> onProgress)
        {
            BenchmarkConfigData safeConfig = config.Clone();

            if (!benchmark.IsScenarioSupported(safeConfig.Scenario))
            {
                return CreateResult(
                    benchmark,
                    safeConfig,
                    new double[safeConfig.MeasuredRuns],
                    0,
                    0,
                    0,
                    false,
                    "Scenario is not applicable for this structure.");
            }

            for (int i = 0; i < safeConfig.WarmupRuns; i++)
            {
                onProgress?.Invoke(new BenchmarkProgress(
                    "Warmup",
                    i + 1,
                    safeConfig.WarmupRuns));

                ProjectileDataset dataset = _datasetGenerator.Generate(
                    safeConfig,
                    i);

                benchmark.Prepare(safeConfig, dataset);
                _stopwatch.Restart();
                benchmark.RunScenario(safeConfig, _stopwatch);
                if (_stopwatch.IsRunning)
                {
                    _stopwatch.Stop();
                }
                benchmark.Cleanup();
            }

            double[] measurements = new double[safeConfig.MeasuredRuns];
            int checksum = 0;
            long allocatedBytes = 0;
            int gcCollections = 0;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            for (int i = 0; i < safeConfig.MeasuredRuns; i++)
            {
                onProgress?.Invoke(new BenchmarkProgress(
                    "Measured run",
                    i + 1,
                    safeConfig.MeasuredRuns));

                ProjectileDataset dataset = _datasetGenerator.Generate(
                    safeConfig,
                    i);

                benchmark.Prepare(safeConfig, dataset);

                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                int gcBefore = GetTotalGCCollections();

                _stopwatch.Restart();
                checksum += benchmark.RunScenario(safeConfig, _stopwatch);

                if (_stopwatch.IsRunning)
                {
                    _stopwatch.Stop();
                }

                int gcAfter = GetTotalGCCollections();
                long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();

                benchmark.Cleanup();

                measurements[i] = _stopwatch.Elapsed.TotalMilliseconds;
                allocatedBytes += allocatedAfter - allocatedBefore;
                gcCollections += gcAfter - gcBefore;
            }

            return CreateResult(
                benchmark,
                safeConfig,
                measurements,
                allocatedBytes,
                gcCollections,
                checksum,
                true,
                "Completed.");
        }

        private BenchmarkResult CreateResult(
            IProjectileStorageBenchmark benchmark,
            BenchmarkConfigData config,
            double[] measurements,
            long allocatedBytes,
            int gcCollections,
            int checksum,
            bool isSupported,
            string message)
        {
            return new BenchmarkResult(
                benchmark.StructureName,
                config.Scenario.ToString(),
                config.ObjectCount,
                config.OperationCount,
                CalculateAverage(measurements),
                CalculateMedian(measurements),
                CalculateStandardDeviation(measurements),
                CalculateMin(measurements),
                CalculateMax(measurements),
                allocatedBytes,
                gcCollections,
                checksum,
                isSupported,
                message,
                config);
        }

        private int GetTotalGCCollections()
        {
            return
                GC.CollectionCount(0) +
                GC.CollectionCount(1) +
                GC.CollectionCount(2);
        }

        private double CalculateAverage(double[] values)
        {
            if (values.Length == 0)
            {
                return 0.0;
            }

            double sum = 0.0;

            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
            }

            return sum / values.Length;
        }

        private double CalculateMedian(double[] values)
        {
            if (values.Length == 0)
            {
                return 0.0;
            }

            double[] copy = new double[values.Length];
            Array.Copy(values, copy, values.Length);
            Array.Sort(copy);

            int middle = copy.Length / 2;

            if (copy.Length % 2 == 0)
            {
                return (copy[middle - 1] + copy[middle]) / 2.0;
            }

            return copy[middle];
        }

        private double CalculateStandardDeviation(double[] values)
        {
            if (values.Length == 0)
            {
                return 0.0;
            }

            double average = CalculateAverage(values);
            double sum = 0.0;

            for (int i = 0; i < values.Length; i++)
            {
                double difference = values[i] - average;
                sum += difference * difference;
            }

            return Math.Sqrt(sum / values.Length);
        }

        private double CalculateMin(double[] values)
        {
            if (values.Length == 0)
            {
                return 0.0;
            }

            double min = double.MaxValue;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < min)
                {
                    min = values[i];
                }
            }

            return min;
        }

        private double CalculateMax(double[] values)
        {
            if (values.Length == 0)
            {
                return 0.0;
            }

            double max = double.MinValue;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > max)
                {
                    max = values[i];
                }
            }

            return max;
        }
    }
}
