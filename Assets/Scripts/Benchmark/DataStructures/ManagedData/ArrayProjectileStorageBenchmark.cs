using System.Diagnostics;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile.Data;
using UnityEngine;

namespace Benchmark.Benchmarks
{
    public class ArrayProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private ProjectileData[] _items;
        private ProjectileDataset _dataset;
        private int _count;

        public string StructureName => "Array";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return scenario != BenchmarkScenario.JobsBurstMassUpdate &&
                   scenario != BenchmarkScenario.ParallelWriteResults;
        }

        public void Prepare(BenchmarkConfigData config, ProjectileDataset dataset)
        {
            _dataset = dataset;

            int length = dataset == null ? 0 : dataset.Count;
            _items = new ProjectileData[length];
            _count = length;

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

                case BenchmarkScenario.FullWaveCycle:
                    return RunFullWaveCycle(config);

                default:
                    return 0;
            }
        }

        public void Cleanup()
        {
            _items = null;
            _dataset = null;
            _count = 0;
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

            if (config.PreallocateCapacity)
            {
                ProjectileData[] target = new ProjectileData[operationCount];
                int count = 0;

                for (int i = 0; i < operationCount; i++)
                {
                    ProjectileData item = GetDatasetItem(i);
                    target[count] = item;
                    count++;
                    checksum += item.Id;
                }

                _items = target;
                _count = count;

                return checksum + _count;
            }

            ProjectileData[] dynamicArray = new ProjectileData[0];

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                ProjectileData[] expanded = new ProjectileData[dynamicArray.Length + 1];

                for (int j = 0; j < dynamicArray.Length; j++)
                {
                    expanded[j] = dynamicArray[j];
                }

                expanded[expanded.Length - 1] = item;
                dynamicArray = expanded;
                checksum += item.Id;
            }

            _items = dynamicArray;
            _count = dynamicArray.Length;

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
            ProjectileData[] target = new ProjectileData[operationCount];
            int checksum = 0;

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                target[i] = item;
                checksum += item.Id;
            }

            _items = target;
            _count = operationCount;

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

        private int RunFullWaveCycle(BenchmarkConfigData config)
        {
            int checksum = 0;
            int maxActive = Mathf.Max(1, config.WaveCount * config.ProjectilesPerWave);
            ProjectileData[] active = new ProjectileData[maxActive];
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
                        active[index] = active[activeCount - 1];
                        activeCount--;
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            _items = active;
            _count = activeCount;

            return checksum + activeCount;
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
    }
}