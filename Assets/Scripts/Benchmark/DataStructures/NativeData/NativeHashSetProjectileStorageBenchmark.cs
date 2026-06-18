using System.Diagnostics;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Benchmark.Benchmarks
{
    public class NativeHashSetProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private NativeHashSet<int> _items;
        private NativeArray<int> _lookupTargetIds;
        private NativeArray<int> _lookupResults;
        private ProjectileDataset _dataset;
        private bool _isCreated;
        private bool _isLookupDataCreated;

        public string StructureName => "NativeHashSet";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return
                scenario == BenchmarkScenario.RemoveElement ||
                scenario == BenchmarkScenario.SearchById ||
                scenario == BenchmarkScenario.ContainsElement ||
                scenario == BenchmarkScenario.ClearCollection;
        }

        public void Prepare(BenchmarkConfigData config, ProjectileDataset dataset)
        {
            Cleanup();

            _dataset = dataset;

            int length = dataset == null ? 0 : dataset.Count;

            int capacity = config.PreallocateCapacity ? (length < 1 ? 1 : length) : 1;

            _items = new NativeHashSet<int>(capacity, Allocator.Persistent);
            _isCreated = true;

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

                default:
                    checksum = 0;

                    break;
            }

            global::Benchmark.Core.BenchmarkTimer.Stop(stopwatch);

            return checksum;
        }

        public void Cleanup()
        {
            if (_isCreated && _items.IsCreated)
            {
                _items.Dispose();
            }

            DisposeLookupData();

            _dataset = null;
            _isCreated = false;
        }

        private int RunRemoveElement(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;
            int remaining = _items.Count;

            for (int i = 0; i < operations; i++)
            {
                if (remaining == 0)
                {
                    break;
                }

                int id = GetTargetId(i);

                if (_items.Remove(id))
                {
                    checksum += id;
                    remaining--;
                }
            }

            return checksum + remaining;
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

        private void PrepareLookupData(BenchmarkConfigData config)
        {
            DisposeLookupData();

            int operations = GetSafeOperationCount(config);
            _lookupTargetIds = new NativeArray<int>(operations, Allocator.Persistent);
            _lookupResults = new NativeArray<int>(operations, Allocator.Persistent);
            _isLookupDataCreated = true;

            for (int i = 0; i < operations; i++)
            {
                _lookupTargetIds[i] = GetTargetId(i);
                _lookupResults[i] = 0;
            }
        }

        private void DisposeLookupData()
        {
            if (_isLookupDataCreated)
            {
                if (_lookupTargetIds.IsCreated)
                {
                    _lookupTargetIds.Dispose();
                }

                if (_lookupResults.IsCreated)
                {
                    _lookupResults.Dispose();
                }
            }

            _isLookupDataCreated = false;
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