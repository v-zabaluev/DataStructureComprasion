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
    public class NativeHashMapProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private NativeHashMap<int, ProjectileData> _items;
        private NativeArray<int> _lookupTargetIds;
        private NativeArray<int> _lookupResults;
        private NativeArray<ProjectileData> _buildSource;
        private ProjectileDataset _dataset;
        private bool _isCreated;
        private bool _isLookupDataCreated;
        private bool _isBuildSourceCreated;

        public string StructureName => "NativeHashMap";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return true;
        }

        public void Prepare(BenchmarkConfigData config, ProjectileDataset dataset)
        {
            Cleanup();

            _dataset = dataset;

            int length = dataset == null ? 0 : dataset.Count;
            int capacity = config.PreallocateCapacity ? Mathf.Max(1, length) : 1;

            _items = new NativeHashMap<int, ProjectileData>(capacity, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < length; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                _items[item.Id] = item;
            }

            if (config.Scenario == BenchmarkScenario.BatchIdLookup)
            {
                PrepareLookupData(config);
            }

            if (config.Scenario == BenchmarkScenario.JobStructureBuild)
            {
                PrepareBuildSource(config);
            }
        }

        public int RunScenario(BenchmarkConfigData config, Stopwatch stopwatch)
        {
            global::Benchmark.Core.BenchmarkTimer.Restart(stopwatch);

            int checksum;

            switch (config.Scenario)
            {
                case BenchmarkScenario.SequentialIteration:
                    checksum = RunSequentialIteration();
                    break;

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

                case BenchmarkScenario.UpdateAll:
                    checksum = RunUpdateAll(config);
                    break;

                case BenchmarkScenario.UpdateOne:
                    checksum = RunUpdateOne(config);
                    break;

                case BenchmarkScenario.ClearCollection:
                    checksum = RunClearCollection();
                    break;

                case BenchmarkScenario.MassFill:
                    checksum = RunMassFill(config);
                    break;

                case BenchmarkScenario.EffectArea:
                    checksum = RunEffectArea(config);
                    break;

                case BenchmarkScenario.FullWaveCycle:
                    checksum = RunFullWaveCycle(config);
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
            DisposeBuildSource();

            _dataset = null;
            _isCreated = false;
        }

        private int RunSequentialIteration()
        {
            int checksum = 0;

            foreach (KVPair<int, ProjectileData> pair in _items)
            {
                ProjectileData item = pair.Value;
                checksum += item.Id;
                checksum += Mathf.FloorToInt(item.Position.x);
                checksum += Mathf.FloorToInt(item.Position.y);
            }

            return checksum;
        }

        private int RunAddElements(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);
            int checksum = 0;

            DisposeCurrentMap();

            int capacity = config.PreallocateCapacity ? operationCount : 1;
            _items = new NativeHashMap<int, ProjectileData>(capacity, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                _items[item.Id] = item;
                checksum += item.Id;
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

                int targetId = GetTargetId(i);

                if (_items.TryGetValue(targetId, out ProjectileData item))
                {
                    checksum += item.Id;
                    _items.Remove(targetId);
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
                int targetId = GetTargetId(i);

                if (_items.TryGetValue(targetId, out ProjectileData item))
                {
                    checksum += item.Id;
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
                int targetId = GetTargetId(i);

                if (_items.ContainsKey(targetId))
                {
                    checksum++;
                }
            }

            return checksum;
        }

        private int RunUpdateAll(BenchmarkConfigData config)
        {
            int checksum = 0;
            int count = _dataset == null ? 0 : _dataset.Count;

            for (int id = 0; id < count; id++)
            {
                if (_items.TryGetValue(id, out ProjectileData item))
                {
                    item.Update(config.DeltaTime);
                    _items[id] = item;

                    checksum += Mathf.FloorToInt(item.Position.x);
                    checksum += Mathf.FloorToInt(item.Position.y);
                }
            }

            return checksum;
        }

        private int RunUpdateOne(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                int targetId = GetTargetId(i);

                if (_items.TryGetValue(targetId, out ProjectileData item))
                {
                    item.Update(config.DeltaTime);
                    _items[targetId] = item;

                    checksum += item.Id;
                    checksum += Mathf.FloorToInt(item.LifeTime * 1000f);
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

            DisposeCurrentMap();

            int capacity = config.PreallocateCapacity ? operationCount : 1;
            _items = new NativeHashMap<int, ProjectileData>(capacity, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                _items[item.Id] = item;
                checksum += item.Id;
            }

            return checksum + _items.Count;
        }

        private int RunEffectArea(BenchmarkConfigData config)
        {
            int checksum = 0;
            int insideCount = 0;

            foreach (KVPair<int, ProjectileData> pair in _items)
            {
                ProjectileData item = pair.Value;

                if (item.IsInsideCircle(config.EffectCenter, config.EffectRadius))
                {
                    insideCount++;
                    checksum += item.Id;
                }
            }

            return checksum + insideCount;
        }

        private int RunFullWaveCycle(BenchmarkConfigData config)
        {
            int checksum = 0;
            int maxActive = Mathf.Max(1, config.WaveCount * config.ProjectilesPerWave);

            NativeHashMap<int, ProjectileData> active = new NativeHashMap<int, ProjectileData>(
                config.PreallocateCapacity ? maxActive : 1,
                Allocator.Persistent);

            int spawnedCount = 0;

            for (int wave = 0; wave < config.WaveCount; wave++)
            {
                for (int i = 0; i < config.ProjectilesPerWave; i++)
                {
                    ProjectileData item = GetDatasetItem(spawnedCount);
                    active[item.Id] = item;
                    spawnedCount++;

                    checksum += item.Id;
                }

                for (int id = 0; id < spawnedCount; id++)
                {
                    if (active.TryGetValue(id, out ProjectileData item))
                    {
                        item.Update(config.DeltaTime);
                        checksum += Mathf.FloorToInt(item.Position.x + item.Position.y);

                        if (item.IsExpired())
                        {
                            checksum += item.Id;
                            active.Remove(id);
                        }
                        else
                        {
                            active[id] = item;
                        }
                    }
                }
            }

            DisposeCurrentMap();
            _items = active;
            _isCreated = true;

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

            if (!_isBuildSourceCreated || !_buildSource.IsCreated)
            {
                return 0;
            }

            int checksum = 0;

            DisposeCurrentMap();

            int capacity = config.PreallocateCapacity ? operationCount : 1;
            _items = new NativeHashMap<int, ProjectileData>(capacity, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = _buildSource[i];
                _items[item.Id] = item;
                checksum += item.Id;
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

        private void PrepareBuildSource(BenchmarkConfigData config)
        {
            DisposeBuildSource();

            int operationCount = GetSafeOperationCount(config);
            _buildSource = new NativeArray<ProjectileData>(operationCount, Allocator.Persistent);
            _isBuildSourceCreated = true;

            for (int i = 0; i < operationCount; i++)
            {
                _buildSource[i] = GetDatasetItem(i);
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

        private void DisposeBuildSource()
        {
            if (_isBuildSourceCreated && _buildSource.IsCreated)
            {
                _buildSource.Dispose();
            }

            _isBuildSourceCreated = false;
        }

        private void DisposeCurrentMap()
        {
            if (_isCreated && _items.IsCreated)
            {
                _items.Dispose();
            }

            _isCreated = false;
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
        private struct BatchIdLookupJob : IJobParallelFor
        {
            [ReadOnly] public NativeHashMap<int, ProjectileData> Items;
            [ReadOnly] public NativeArray<int> TargetIds;
            public NativeArray<int> Results;

            public void Execute(int index)
            {
                int id = TargetIds[index];
                Results[index] = Items.ContainsKey(id) ? id : 0;
            }
        }
    }
}
