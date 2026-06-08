using System.Collections.Generic;
using System.Diagnostics;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile.Data;

namespace Benchmark.Benchmarks
{
    public class HashSetProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private HashSet<int> _items;
        private ProjectileDataset _dataset;

        public string StructureName => "HashSet";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return
                scenario == BenchmarkScenario.AddElements ||
                scenario == BenchmarkScenario.RemoveElement ||
                scenario == BenchmarkScenario.SearchById ||
                scenario == BenchmarkScenario.ContainsElement ||
                scenario == BenchmarkScenario.ClearCollection ||
                scenario == BenchmarkScenario.MassFill ||
                scenario == BenchmarkScenario.BatchIdLookup ||
                scenario == BenchmarkScenario.JobStructureBuild;
        }

        public void Prepare(BenchmarkConfigData config, ProjectileDataset dataset)
        {
            _dataset = dataset;

            int length = dataset == null ? 0 : dataset.Count;

            if (config.PreallocateCapacity)
            {
                _items = new HashSet<int>(length);
            }
            else
            {
                _items = new HashSet<int>();
            }

            for (int i = 0; i < length; i++)
            {
                _items.Add(i);
            }
        }

        public int RunScenario(BenchmarkConfigData config, Stopwatch stopwatch)
        {
            global::Benchmark.Core.BenchmarkTimer.Restart(stopwatch);

            int checksum;

            switch (config.Scenario)
            {
                case BenchmarkScenario.AddElements:
                    checksum = RunAddElements(config);
                    break;

                case BenchmarkScenario.RemoveElement:
                    checksum = RunRemoveElement(config);
                    break;

                case BenchmarkScenario.SearchById:
                    checksum = RunSearchById(config);
                    break;

                case BenchmarkScenario.ContainsElement:
                    checksum = RunContainsElement(config);
                    break;

                case BenchmarkScenario.ClearCollection:
                    checksum = RunClearCollection();
                    break;

                case BenchmarkScenario.MassFill:
                    checksum = RunMassFill(config);
                    break;

                case BenchmarkScenario.BatchIdLookup:
                    checksum = RunBatchIdLookup(config);
                    break;

                case BenchmarkScenario.JobStructureBuild:
                    checksum = RunJobStructureBuild(config);
                    break;

                default:
                    checksum = 0;
                    break;
            }

            global::Benchmark.Core.BenchmarkTimer.Stop(stopwatch);
            return checksum;
        }

        public void Cleanup()
        {
            _items = null;
            _dataset = null;
        }

        private int RunAddElements(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);
            int checksum = 0;

            HashSet<int> target;

            if (config.PreallocateCapacity)
            {
                target = new HashSet<int>(operationCount);
            }
            else
            {
                target = new HashSet<int>();
            }

            for (int i = 0; i < operationCount; i++)
            {
                int id = GetUniqueId(i);
                target.Add(id);
                checksum += id;
            }

            _items = target;

            return checksum + _items.Count;
        }

        private int RunRemoveElement(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                if (_items.Count == 0)
                {
                    break;
                }

                int id = GetTargetId(i);

                if (_items.Remove(id))
                {
                    checksum += id;
                }
            }

            return checksum + _items.Count;
        }

        private int RunSearchById(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                int id = GetTargetId(i);

                if (_items.Contains(id))
                {
                    checksum += id;
                }
                else
                {
                    checksum--;
                }
            }

            return checksum;
        }

        private int RunContainsElement(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                int id = GetTargetId(i);

                if (_items.Contains(id))
                {
                    checksum++;
                }
            }

            return checksum;
        }

        private int RunClearCollection()
        {
            int checksum = _items.Count;
            _items.Clear();

            return checksum;
        }

        private int RunMassFill(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);
            int checksum = 0;

            HashSet<int> target;

            if (config.PreallocateCapacity)
            {
                target = new HashSet<int>(operationCount);
            }
            else
            {
                target = new HashSet<int>();
            }

            for (int i = 0; i < operationCount; i++)
            {
                int id = GetUniqueId(i);
                target.Add(id);
                checksum += id;
            }

            _items = target;

            return checksum + _items.Count;
        }

        private int RunBatchIdLookup(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                int id = GetTargetId(i);

                if (_items.Contains(id))
                {
                    checksum += id;
                }
            }

            return checksum;
        }

        private int RunJobStructureBuild(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);
            int checksum = 0;

            HashSet<int> target = config.PreallocateCapacity
                ? new HashSet<int>(operationCount)
                : new HashSet<int>();

            for (int i = 0; i < operationCount; i++)
            {
                int id = GetUniqueId(i);
                target.Add(id);
                checksum += id;
            }

            _items = target;

            return checksum + _items.Count;
        }

        private int GetTargetId(int operationIndex)
        {
            if (_dataset == null || _dataset.Count == 0)
            {
                return -1;
            }

            return operationIndex % _dataset.Count;
        }

        private int GetUniqueId(int operationIndex)
        {
            return operationIndex;
        }

        private int GetSafeOperationCount(BenchmarkConfigData config)
        {
            if (config.OperationCount < 1)
            {
                return 1;
            }

            return config.OperationCount;
        }
    }
}