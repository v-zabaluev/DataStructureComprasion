using System.Diagnostics;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile.Data;
using Unity.Collections;

namespace Benchmark.Benchmarks
{
    public class NativeHashSetProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private NativeHashSet<int> _items;
        private ProjectileDataset _dataset;
        private bool _isCreated;

        public string StructureName => "NativeHashSet";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return
                scenario == BenchmarkScenario.AddElements ||
                scenario == BenchmarkScenario.RemoveElement ||
                scenario == BenchmarkScenario.SearchById ||
                scenario == BenchmarkScenario.ContainsElement ||
                scenario == BenchmarkScenario.ClearCollection ||
                scenario == BenchmarkScenario.MassFill;
        }

        public void Prepare(BenchmarkConfigData config, ProjectileDataset dataset)
        {
            Cleanup();

            _dataset = dataset;

            int length = dataset == null ? 0 : dataset.Count;
            int capacity = config.PreallocateCapacity ? length : 1;

            _items = new NativeHashSet<int>(capacity, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < length; i++)
            {
                _items.Add(i);
            }
        }

        public int RunScenario(BenchmarkConfigData config, Stopwatch stopwatch)
        {
            switch (config.Scenario)
            {
                case BenchmarkScenario.AddElements:
                    return RunAddElements(config);

                case BenchmarkScenario.RemoveElement:
                    return RunRemoveElement(config);

                case BenchmarkScenario.SearchById:
                    return RunSearchById(config);

                case BenchmarkScenario.ContainsElement:
                    return RunContainsElement(config);

                case BenchmarkScenario.ClearCollection:
                    return RunClearCollection();

                case BenchmarkScenario.MassFill:
                    return RunMassFill(config);

                default:
                    return 0;
            }
        }

        public void Cleanup()
        {
            if (_isCreated && _items.IsCreated)
            {
                _items.Dispose();
            }

            _dataset = null;
            _isCreated = false;
        }

        private int RunAddElements(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);
            int checksum = 0;

            DisposeCurrentSet();

            int capacity = config.PreallocateCapacity ? operationCount : 1;
            _items = new NativeHashSet<int>(capacity, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < operationCount; i++)
            {
                int id = GetUniqueId(i);
                _items.Add(id);
                checksum += id;
            }

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

            DisposeCurrentSet();

            int capacity = config.PreallocateCapacity ? operationCount : 1;
            _items = new NativeHashSet<int>(capacity, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < operationCount; i++)
            {
                int id = GetUniqueId(i);
                _items.Add(id);
                checksum += id;
            }

            return checksum + _items.Count;
        }

        private void DisposeCurrentSet()
        {
            if (_isCreated && _items.IsCreated)
            {
                _items.Dispose();
            }

            _isCreated = false;
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