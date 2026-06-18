using System.Diagnostics;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Benchmark.Benchmarks
{
    public class NativeHashMapJobProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private NativeHashMap<int, ProjectileData> _items;
        private NativeArray<ProjectileData> _source;
        private NativeArray<int> _targetIds;
        private NativeArray<int> _results;
        private ProjectileDataset _dataset;
        private int _count;
        private bool _isItemsCreated;
        private bool _isSourceCreated;
        private bool _isLookupCreated;

        public string StructureName => "NativeHashMapJob";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return
                scenario == BenchmarkScenario.SequentialIteration ||
                scenario == BenchmarkScenario.AddElements ||
                scenario == BenchmarkScenario.SearchById ||
                scenario == BenchmarkScenario.ContainsElement ||
                scenario == BenchmarkScenario.UpdateAll ||
                scenario == BenchmarkScenario.UpdateOne;
        }

        public void Prepare(BenchmarkConfigData config, ProjectileDataset dataset)
        {
            Cleanup();
            _dataset = dataset;

            if (config.Scenario == BenchmarkScenario.AddElements)
            {
                PrepareSource(config);

                return;
            }

            PrepareItems(config);

            if (config.Scenario == BenchmarkScenario.SearchById ||
                config.Scenario == BenchmarkScenario.ContainsElement ||
                config.Scenario == BenchmarkScenario.UpdateOne)
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
                case BenchmarkScenario.SequentialIteration:
                    checksum = RunSequentialIterationJob();

                    break;

                case BenchmarkScenario.AddElements:
                    checksum = RunAddElementsJob(config);

                    break;

                case BenchmarkScenario.SearchById:
                    checksum = RunSearchByIdJob(config);

                    break;

                case BenchmarkScenario.ContainsElement:
                    checksum = RunContainsElementJob(config);

                    break;

                case BenchmarkScenario.UpdateAll:
                    checksum = RunUpdateAllJob(config);

                    break;

                case BenchmarkScenario.UpdateOne:
                    checksum = RunUpdateOneJob(config);

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
            DisposeItems();
            DisposeSource();
            DisposeLookupData();
            _dataset = null;
            _count = 0;
        }

        private int RunSequentialIterationJob()
        {
            EnsureResults(1);

            SequentialIterationJob job = new SequentialIterationJob
            {
                Items = _items,
                Results = _results
            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            return _results[0];
        }

        private int RunAddElementsJob(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);

            if (!_isSourceCreated || !_source.IsCreated)
            {
                return 0;
            }

            int capacity = config.PreallocateCapacity ? operationCount : 1;
            _items = new NativeHashMap<int, ProjectileData>(capacity, Allocator.Persistent);
            _isItemsCreated = true;
            _count = operationCount;

            AddElementsJob job = new AddElementsJob
            {
                Source = _source,
                Items = _items,
                Count = operationCount
            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            int checksum = 0;

            for (int i = 0; i < operationCount; i++)
            {
                checksum += _source[i].Id;
            }

            return checksum + operationCount;
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

        private int RunUpdateAllJob(BenchmarkConfigData config)
        {
            EnsureResults(1);

            UpdateAllJob job = new UpdateAllJob
            {
                DeltaTime = config.DeltaTime,
                Items = _items,
                Count = _count,
                Results = _results
            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            return _results[0];
        }

        private int RunUpdateOneJob(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);

            UpdateOneJob job = new UpdateOneJob
            {
                DeltaTime = config.DeltaTime,
                Items = _items,
                TargetIds = _targetIds,
                Results = _results,
                Operations = operations
            };

            JobHandle handle = job.Schedule();
            handle.Complete();

            return SumResults(operations);
        }

        private void PrepareItems(BenchmarkConfigData config)
        {
            int length = _dataset == null ? 0 : _dataset.Count;
            int capacity = config.PreallocateCapacity ? Mathf.Max(1, length) : 1;
            _count = length;

            _items = new NativeHashMap<int, ProjectileData>(capacity, Allocator.Persistent);
            _isItemsCreated = true;

            for (int i = 0; i < length; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                _items[item.Id] = item;
            }
        }

        private void PrepareSource(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);
            _source = new NativeArray<ProjectileData>(operationCount, Allocator.Persistent);
            _isSourceCreated = true;

            for (int i = 0; i < operationCount; i++)
            {
                _source[i] = GetDatasetItem(i);
            }
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

        private void EnsureResults(int length)
        {
            if (_results.IsCreated && _results.Length >= length)
            {
                return;
            }

            if (_results.IsCreated)
            {
                _results.Dispose();
            }

            _results = new NativeArray<int>(length, Allocator.Persistent);
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

        private void DisposeItems()
        {
            if (_isItemsCreated && _items.IsCreated)
            {
                _items.Dispose();
            }

            _isItemsCreated = false;
        }

        private void DisposeSource()
        {
            if (_isSourceCreated && _source.IsCreated)
            {
                _source.Dispose();
            }

            _isSourceCreated = false;
        }

        private void DisposeLookupData()
        {
            if (_isLookupCreated)
            {
                if (_targetIds.IsCreated)
                {
                    _targetIds.Dispose();
                }
            }

            if (_results.IsCreated)
            {
                _results.Dispose();
            }

            _isLookupCreated = false;
        }

        private ProjectileData GetDatasetItem(int index)
        {
            if (_dataset == null || _dataset.Count == 0)
            {
                return default;
            }

            ProjectileData item = _dataset.Get(index % _dataset.Count);
            item.Id = index;

            return item;
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
        private struct AddElementsJob : IJob
        {
            [ReadOnly] public NativeArray<ProjectileData> Source;
            public NativeHashMap<int, ProjectileData> Items;
            public int Count;

            public void Execute()
            {
                for (int i = 0; i < Count; i++)
                {
                    ProjectileData item = Source[i];
                    Items[item.Id] = item;
                }
            }
        }

        [BurstCompile]
        private struct SequentialIterationJob : IJob
        {
            [ReadOnly] public NativeHashMap<int, ProjectileData> Items;
            public NativeArray<int> Results;

            public void Execute()
            {
                int checksum = 0;

                foreach (KVPair<int, ProjectileData> pair in Items)
                {
                    ProjectileData item = pair.Value;
                    checksum += item.Id;
                    checksum += Mathf.FloorToInt(item.Position.x);
                    checksum += Mathf.FloorToInt(item.Position.y);
                }

                Results[0] = checksum;
            }
        }

        [BurstCompile]
        private struct SearchByIdJob : IJobParallelFor
        {
            [ReadOnly] public NativeHashMap<int, ProjectileData> Items;
            [ReadOnly] public NativeArray<int> TargetIds;
            public NativeArray<int> Results;

            public void Execute(int index)
            {
                int targetId = TargetIds[index];

                if (Items.TryGetValue(targetId, out ProjectileData item))
                {
                    Results[index] = item.Id;
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
            [ReadOnly] public NativeHashMap<int, ProjectileData> Items;
            [ReadOnly] public NativeArray<int> TargetIds;
            public NativeArray<int> Results;

            public void Execute(int index)
            {
                Results[index] = Items.ContainsKey(TargetIds[index]) ? 1 : 0;
            }
        }

        [BurstCompile]
        private struct UpdateAllJob : IJob
        {
            public float DeltaTime;
            public NativeHashMap<int, ProjectileData> Items;
            public int Count;
            public NativeArray<int> Results;

            public void Execute()
            {
                int checksum = 0;

                for (int id = 0; id < Count; id++)
                {
                    if (Items.TryGetValue(id, out ProjectileData item))
                    {
                        item.Update(DeltaTime);
                        Items[id] = item;

                        checksum += Mathf.FloorToInt(item.Position.x);
                        checksum += Mathf.FloorToInt(item.Position.y);
                    }
                }

                Results[0] = checksum;
            }
        }

        [BurstCompile]
        private struct UpdateOneJob : IJob
        {
            public float DeltaTime;
            public NativeHashMap<int, ProjectileData> Items;
            [ReadOnly] public NativeArray<int> TargetIds;
            public NativeArray<int> Results;
            public int Operations;

            public void Execute()
            {
                for (int i = 0; i < Operations; i++)
                {
                    int targetId = TargetIds[i];

                    if (Items.TryGetValue(targetId, out ProjectileData item))
                    {
                        item.Update(DeltaTime);
                        Items[targetId] = item;

                        Results[i] = Mathf.FloorToInt(item.Position.x) +
                                     Mathf.FloorToInt(item.Position.y);
                    }
                    else
                    {
                        Results[i] = -1;
                    }
                }
            }
        }
    }
}