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
    public class NativeArrayProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private NativeArray<ProjectileData> _items;
        private ProjectileDataset _dataset;
        private int _count;
        private bool _isCreated;

        public string StructureName => "NativeArray";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return scenario != BenchmarkScenario.ParallelWriteResults;
        }

        public void Prepare(BenchmarkConfigData config, ProjectileDataset dataset)
        {
            Cleanup();

            _dataset = dataset;

            int length = dataset == null ? 0 : dataset.Count;
            _count = length;

            if (length <= 0)
            {
                return;
            }

            _items = new NativeArray<ProjectileData>(length, Allocator.Persistent);
            _isCreated = true;

            for (int i = 0; i < length; i++)
            {
                _items[i] = GetDatasetItem(i);
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
            _count = 0;
            _isCreated = false;
        }

        private int RunSequentialIteration()
        {
            int checksum = 0;

            for (int i = 0; i < _count; i++)
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

            DisposeCurrentArray();

            if (config.PreallocateCapacity)
            {
                _items = new NativeArray<ProjectileData>(operationCount, Allocator.Persistent);
                _isCreated = true;
                _count = operationCount;

                for (int i = 0; i < operationCount; i++)
                {
                    ProjectileData item = GetDatasetItem(i);
                    _items[i] = item;
                    checksum += item.Id;
                }

                return checksum + _count;
            }

            NativeArray<ProjectileData> dynamicArray = default;
            int currentLength = 0;

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                NativeArray<ProjectileData> expanded = new NativeArray<ProjectileData>(currentLength + 1, Allocator.Persistent);

                for (int j = 0; j < currentLength; j++)
                {
                    expanded[j] = dynamicArray[j];
                }

                expanded[currentLength] = item;

                if (dynamicArray.IsCreated)
                {
                    dynamicArray.Dispose();
                }

                dynamicArray = expanded;
                currentLength++;
                checksum += item.Id;
            }

            _items = dynamicArray;
            _isCreated = _items.IsCreated;
            _count = currentLength;

            return checksum + _count;
        }

        private int RunRemoveElement(BenchmarkConfigData config)
        {
            int operations = Mathf.Min(GetSafeOperationCount(config), _count);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                if (_count == 0)
                {
                    break;
                }

                int index = (_count - 1) / 2;
                checksum += _items[index].Id;

                if (config.PreserveOrderOnRemove)
                {
                    for (int j = index; j < _count - 1; j++)
                    {
                        _items[j] = _items[j + 1];
                    }
                }
                else
                {
                    _items[index] = _items[_count - 1];
                }

                _count--;
                _items[_count] = default;
            }

            return checksum + _count;
        }

        private int RunSearchById(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                int targetId = GetTargetId(i);
                int foundIndex = -1;

                for (int j = 0; j < _count; j++)
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

                for (int j = 0; j < _count; j++)
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

            for (int i = 0; i < _count; i++)
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

            if (_count == 0)
            {
                return checksum;
            }

            for (int i = 0; i < operations; i++)
            {
                int index = i % _count;
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
            int checksum = _count;

            for (int i = 0; i < _count; i++)
            {
                _items[i] = default;
            }

            _count = 0;

            return checksum;
        }

        private int RunMassFill(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);
            int checksum = 0;

            DisposeCurrentArray();

            _items = new NativeArray<ProjectileData>(operationCount, Allocator.Persistent);
            _isCreated = true;
            _count = operationCount;

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                _items[i] = item;
                checksum += item.Id;
            }

            return checksum + _count;
        }

        private int RunEffectArea(BenchmarkConfigData config)
        {
            int checksum = 0;
            int insideCount = 0;

            for (int i = 0; i < _count; i++)
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
            if (_count == 0)
            {
                return 0;
            }

            UpdateProjectilesJob job = new UpdateProjectilesJob
            {
                DeltaTime = config.DeltaTime,
                Items = _items
            };

            JobHandle handle = job.Schedule(_count, 64);
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
            NativeArray<ProjectileData> active = new NativeArray<ProjectileData>(maxActive, Allocator.Persistent);
            int activeCount = 0;

            for (int wave = 0; wave < config.WaveCount; wave++)
            {
                for (int i = 0; i < config.ProjectilesPerWave; i++)
                {
                    if (activeCount >= active.Length)
                    {
                        break;
                    }

                    ProjectileData item = GetDatasetItem(wave * config.ProjectilesPerWave + i);
                    active[activeCount] = item;
                    activeCount++;
                    checksum += item.Id;
                }

                for (int i = 0; i < activeCount; i++)
                {
                    ProjectileData item = active[i];
                    item.Update(config.DeltaTime);
                    active[i] = item;
                    checksum += Mathf.FloorToInt(item.Position.x + item.Position.y);
                }

                int index = 0;

                while (index < activeCount)
                {
                    if (active[index].IsExpired())
                    {
                        checksum += active[index].Id;

                        if (config.PreserveOrderOnRemove)
                        {
                            for (int j = index; j < activeCount - 1; j++)
                            {
                                active[j] = active[j + 1];
                            }

                            activeCount--;
                            active[activeCount] = default;
                        }
                        else
                        {
                            active[index] = active[activeCount - 1];
                            activeCount--;
                            active[activeCount] = default;
                        }
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            DisposeCurrentArray();
            _items = active;
            _isCreated = true;
            _count = activeCount;

            return checksum + activeCount;
        }

        private void DisposeCurrentArray()
        {
            if (_isCreated && _items.IsCreated)
            {
                _items.Dispose();
            }

            _isCreated = false;
            _count = 0;
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
