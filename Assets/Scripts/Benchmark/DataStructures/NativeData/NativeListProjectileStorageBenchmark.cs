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
    public class NativeListProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private NativeList<ProjectileData> _items;
        private ProjectileDataset _dataset;
        private bool _isCreated;

        public string StructureName => "NativeList";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return scenario != BenchmarkScenario.ParallelWriteResults;
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
        }

        public int RunScenario(BenchmarkConfigData config, Stopwatch stopwatch)
        {
            switch (config.Scenario)
            {
                case BenchmarkScenario.SequentialIteration:
                    return RunSequentialIteration();

                case BenchmarkScenario.AddElements:
                    return RunAddElements(config);

                case BenchmarkScenario.RemoveElement:
                    return RunRemoveElement(config);

                case BenchmarkScenario.SearchById:
                    return RunSearchById(config);

                case BenchmarkScenario.ContainsElement:
                    return RunContainsElement(config);

                case BenchmarkScenario.UpdateAll:
                    return RunUpdateAll(config);

                case BenchmarkScenario.UpdateOne:
                    return RunUpdateOne(config);

                case BenchmarkScenario.ClearCollection:
                    return RunClearCollection();

                case BenchmarkScenario.MassFill:
                    return RunMassFill(config);

                case BenchmarkScenario.EffectArea:
                    return RunEffectArea(config);

                case BenchmarkScenario.JobsBurstMassUpdate:
                    return RunJobsBurstMassUpdate(config, stopwatch);

                case BenchmarkScenario.FullWaveCycle:
                    return RunFullWaveCycle(config);

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

        private int RunJobsBurstMassUpdate(BenchmarkConfigData config, Stopwatch stopwatch)
        {
            if (_items.Length == 0)
            {
                return 0;
            }

            NativeArray<ProjectileData> array = _items.AsArray();

            UpdateProjectilesJob job = new UpdateProjectilesJob
            {
                DeltaTime = config.DeltaTime,
                Items = array
            };

            JobHandle handle = job.Schedule(array.Length, 64);
            handle.Complete();

            if (stopwatch != null && stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }

            return RunSequentialIteration();
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
        private struct UpdateProjectilesJob : IJobParallelFor
        {
            public float DeltaTime;
            public NativeArray<ProjectileData> Items;

            public void Execute(int index)
            {
                ProjectileData item = Items[index];
                item.Update(DeltaTime);
                Items[index] = item;
            }
        }
    }
}
