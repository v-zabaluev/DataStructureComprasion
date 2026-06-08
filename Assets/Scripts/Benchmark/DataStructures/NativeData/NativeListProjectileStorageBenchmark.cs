using System.Diagnostics;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Benchmark.Benchmarks
{
    public class NativeListProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private NativeList<ProjectileData> _items;
        private NativeArray<int> _lookupTargetIds;
        private NativeArray<int> _lookupResults;
        private NativeArray<ProjectileData> _buildSource;
        private ProjectileDataset _dataset;
        private bool _isCreated;
        private bool _isLookupDataCreated;
        private bool _isBuildSourceCreated;

        public string StructureName => "NativeList";

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

            _items = new NativeList<ProjectileData>(capacity, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < length; i++)
            {
                _items.Add(GetDatasetItem(i));
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
                    checksum = RunJobStructureBuild(config, stopwatch);
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

            for (int i = 0; i < _items.Length; i++)
            {
                ProjectileData item = _items[i];
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

            DisposeCurrentList();

            int capacity = config.PreallocateCapacity ? operationCount : 1;
            _items = new NativeList<ProjectileData>(capacity, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                _items.Add(item);
                checksum += item.Id;
            }

            return checksum + _items.Length;
        }

        private int RunRemoveElement(BenchmarkConfigData config)
        {
            int operations = Mathf.Min(GetSafeOperationCount(config), _items.Length);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                if (_items.Length == 0)
                {
                    break;
                }

                int index = (_items.Length - 1) / 2;
                checksum += _items[index].Id;

                if (config.PreserveOrderOnRemove)
                {
                    _items.RemoveAt(index);
                }
                else
                {
                    _items.RemoveAtSwapBack(index);
                }
            }

            return checksum + _items.Length;
        }

        private int RunSearchById(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                int targetId = GetTargetId(i);
                int foundIndex = -1;

                for (int j = 0; j < _items.Length; j++)
                {
                    if (_items[j].Id == targetId)
                    {
                        foundIndex = j;
                        break;
                    }
                }

                checksum += foundIndex;
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
                bool contains = false;

                for (int j = 0; j < _items.Length; j++)
                {
                    if (_items[j].Id == targetId)
                    {
                        contains = true;
                        break;
                    }
                }

                if (contains)
                {
                    checksum++;
                }
            }

            return checksum;
        }

        private int RunUpdateAll(BenchmarkConfigData config)
        {
            int checksum = 0;

            for (int i = 0; i < _items.Length; i++)
            {
                ProjectileData item = _items[i];
                item.Update(config.DeltaTime);
                _items[i] = item;

                checksum += Mathf.FloorToInt(item.Position.x);
                checksum += Mathf.FloorToInt(item.Position.y);
            }

            return checksum;
        }

        private int RunUpdateOne(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;

            if (_items.Length == 0)
            {
                return checksum;
            }

            for (int i = 0; i < operations; i++)
            {
                int index = i % _items.Length;
                ProjectileData item = _items[index];
                item.Update(config.DeltaTime);
                _items[index] = item;

                checksum += item.Id;
                checksum += Mathf.FloorToInt(item.LifeTime * 1000f);
            }

            return checksum;
        }

        private int RunClearCollection()
        {
            int checksum = _items.Length;
            _items.Clear();

            return checksum;
        }

        private int RunMassFill(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);
            int checksum = 0;

            DisposeCurrentList();

            int capacity = config.PreallocateCapacity ? operationCount : 1;
            _items = new NativeList<ProjectileData>(capacity, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                _items.Add(item);
                checksum += item.Id;
            }

            return checksum + _items.Length;
        }

        private int RunEffectArea(BenchmarkConfigData config)
        {
            int checksum = 0;
            int insideCount = 0;

            for (int i = 0; i < _items.Length; i++)
            {
                ProjectileData item = _items[i];

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

            NativeList<ProjectileData> active = new NativeList<ProjectileData>(
                config.PreallocateCapacity ? maxActive : 1,
                Allocator.Persistent);

            for (int wave = 0; wave < config.WaveCount; wave++)
            {
                for (int i = 0; i < config.ProjectilesPerWave; i++)
                {
                    ProjectileData item = GetDatasetItem(wave * config.ProjectilesPerWave + i);
                    active.Add(item);
                    checksum += item.Id;
                }

                for (int i = 0; i < active.Length; i++)
                {
                    ProjectileData item = active[i];
                    item.Update(config.DeltaTime);
                    active[i] = item;
                    checksum += Mathf.FloorToInt(item.Position.x + item.Position.y);
                }

                int index = 0;

                while (index < active.Length)
                {
                    if (active[index].IsExpired())
                    {
                        checksum += active[index].Id;

                        if (config.PreserveOrderOnRemove)
                        {
                            active.RemoveAt(index);
                        }
                        else
                        {
                            active.RemoveAtSwapBack(index);
                        }
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            DisposeCurrentList();
            _items = active;
            _isCreated = true;

            return checksum + _items.Length;
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

        private int RunJobStructureBuild(BenchmarkConfigData config, Stopwatch stopwatch)
        {
            int operationCount = GetSafeOperationCount(config);

            if (!_isBuildSourceCreated || !_buildSource.IsCreated)
            {
                return 0;
            }

            DisposeCurrentList();

            int capacity = config.PreallocateCapacity ? operationCount : 1;
            _items = new NativeList<ProjectileData>(capacity, Allocator.Persistent);
            _items.ResizeUninitialized(operationCount);
            _isCreated = true;

            FillNativeListJob job = new FillNativeListJob
            {
                Source = _buildSource,
                Items = _items
            };

            JobHandle handle = job.Schedule(operationCount, 64);
            handle.Complete();

            if (stopwatch != null && stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }

            return RunSequentialIteration();
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

        private void DisposeCurrentList()
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
            [ReadOnly] public NativeList<ProjectileData> Items;
            [ReadOnly] public NativeArray<int> TargetIds;
            public NativeArray<int> Results;

            public void Execute(int index)
            {
                int targetId = TargetIds[index];
                int result = 0;

                for (int i = 0; i < Items.Length; i++)
                {
                    if (Items[i].Id == targetId)
                    {
                        result = targetId;
                        break;
                    }
                }

                Results[index] = result;
            }
        }

        [BurstCompile]
        private struct FillNativeListJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ProjectileData> Source;
            [NativeDisableParallelForRestriction] public NativeList<ProjectileData> Items;

            public void Execute(int index)
            {
                Items[index] = Source[index];
            }
        }
    }
}
