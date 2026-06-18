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
    public class NativeHashSetJobProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private NativeHashSet<int> _items;
        private NativeArray<int> _targetIds;
        private NativeArray<int> _results;
        private ProjectileDataset _dataset;
        private bool _isCreated;
        private bool _isLookupCreated;

        public string StructureName => "NativeHashSetJob";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return
                scenario == BenchmarkScenario.SearchById ||
                scenario == BenchmarkScenario.ContainsElement;
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

            PrepareLookupData(config);
        }

        public int RunScenario(BenchmarkConfigData config, Stopwatch stopwatch)
        {
            global::Benchmark.Core.BenchmarkTimer.Restart(stopwatch);

            int checksum;

            switch (config.Scenario)
            {
                case BenchmarkScenario.SearchById:
                    checksum = RunSearchByIdJob(config);

                    break;

                case BenchmarkScenario.ContainsElement:
                    checksum = RunContainsElementJob(config);

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

        private int RunSearchByIdJob(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);

            SearchByIdJob job = new SearchByIdJob
            {
                Items = _items,
                TargetIds = _targetIds,
                Results = _results
            };

            JobHandle handle = job.Schedule(operations, 64);
            handle.Complete();

            return SumResults(operations);
        }

        private int RunContainsElementJob(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);

            ContainsElementJob job = new ContainsElementJob
            {
                Items = _items,
                TargetIds = _targetIds,
                Results = _results
            };

            JobHandle handle = job.Schedule(operations, 64);
            handle.Complete();

            return SumResults(operations);
        }

        private void PrepareLookupData(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            _targetIds = new NativeArray<int>(operations, Allocator.Persistent);
            _results = new NativeArray<int>(operations, Allocator.Persistent);
            _isLookupCreated = true;

            for (int i = 0; i < operations; i++)
            {
                _targetIds[i] = GetTargetId(i);
                _results[i] = 0;
            }
        }

        private int SumResults(int length)
        {
            int checksum = 0;

            for (int i = 0; i < length; i++)
            {
                checksum += _results[i];
            }

            return checksum;
        }

        private void DisposeLookupData()
        {
            if (_isLookupCreated)
            {
                if (_targetIds.IsCreated)
                {
                    _targetIds.Dispose();
                }

                if (_results.IsCreated)
                {
                    _results.Dispose();
                }
            }

            _isLookupCreated = false;
        }

        private int GetTargetId(int operationIndex)
        {
            if (_dataset == null || _dataset.Count == 0)
            {
                return -1;
            }

            return operationIndex % _dataset.Count;
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
        private struct SearchByIdJob : IJobParallelFor
        {
            [ReadOnly] public NativeHashSet<int> Items;
            [ReadOnly] public NativeArray<int> TargetIds;
            public NativeArray<int> Results;

            public void Execute(int index)
            {
                int id = TargetIds[index];

                if (Items.Contains(id))
                {
                    Results[index] = id;
                }
                else
                {
                    Results[index] = -1;
                }
            }
        }

        [BurstCompile]
        private struct ContainsElementJob : IJobParallelFor
        {
            [ReadOnly] public NativeHashSet<int> Items;
            [ReadOnly] public NativeArray<int> TargetIds;
            public NativeArray<int> Results;

            public void Execute(int index)
            {
                Results[index] = Items.Contains(TargetIds[index]) ? 1 : 0;
            }
        }
    }
}