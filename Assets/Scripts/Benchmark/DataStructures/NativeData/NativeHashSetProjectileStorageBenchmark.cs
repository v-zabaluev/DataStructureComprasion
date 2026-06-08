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

            if (config.Scenario == BenchmarkScenario.BatchIdLookup)
            {
                PrepareLookupData(config);
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
                    checksum = RunBatchIdLookup(config, stopwatch);
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
            if (_isCreated && _items.IsCreated)
            {
                _items.Dispose();
            }

            DisposeLookupData();

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

        private int RunBatchIdLookup(BenchmarkConfigData config, Stopwatch stopwatch)
        {
            int operations = GetSafeOperationCount(config);

            if (!_isLookupDataCreated || !_lookupTargetIds.IsCreated || !_lookupResults.IsCreated)
            {
                return 0;
            }

            BatchIdLookupJob job = new BatchIdLookupJob
            {
                Items = _items,
                TargetIds = _lookupTargetIds,
                Results = _lookupResults
            };

            JobHandle handle = job.Schedule(operations, 64);
            handle.Complete();

            if (stopwatch != null && stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }

            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                checksum += _lookupResults[i];
            }

            return checksum;
        }

        private int RunJobStructureBuild(BenchmarkConfigData config)
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

        [BurstCompile]
        private struct BatchIdLookupJob : IJobParallelFor
        {
            [ReadOnly] public NativeHashSet<int> Items;
            [ReadOnly] public NativeArray<int> TargetIds;
            public NativeArray<int> Results;

            public void Execute(int index)
            {
                int id = TargetIds[index];
                Results[index] = Items.Contains(id) ? id : 0;
            }
        }
    }
}