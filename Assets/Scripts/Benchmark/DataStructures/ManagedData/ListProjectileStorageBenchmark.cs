using System.Collections.Generic;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile.Data;
using UnityEngine;

namespace Benchmark.Benchmarks
{
    public class ListProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private List<ProjectileData> _items;
        private ProjectileDataset _dataset;

        public string StructureName => "List";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return scenario != BenchmarkScenario.EcsMassUpdateWithJobsBurst;
        }

        public void Prepare(BenchmarkConfigData config, ProjectileDataset dataset)
        {
            _dataset = dataset;

            int length = dataset == null ? 0 : dataset.Count;

            if (config.PreallocateCapacity)
            {
                _items = new List<ProjectileData>(length);
            }
            else
            {
                _items = new List<ProjectileData>();
            }

            for (int i = 0; i < length; i++)
            {
                _items.Add(dataset.Get(i));
            }
        }

        public int RunScenario(BenchmarkConfigData config)
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
        }

        private int RunSequentialIteration()
        {
            int checksum = 0;

            for (int i = 0; i < _items.Count; i++)
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

            List<ProjectileData> target;

            if (config.PreallocateCapacity)
            {
                target = new List<ProjectileData>(operationCount);
            }
            else
            {
                target = new List<ProjectileData>();
            }

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                target.Add(item);
                checksum += item.Id;
            }

            _items = target;

            return checksum + _items.Count;
        }

        private int RunRemoveElement(BenchmarkConfigData config)
        {
            int operations = Mathf.Min(GetSafeOperationCount(config), _items.Count);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                if (_items.Count == 0)
                {
                    break;
                }

                int index = (_items.Count - 1) / 2;
                ProjectileData item = _items[index];
                checksum += item.Id;

                if (config.PreserveOrderOnRemove)
                {
                    _items.RemoveAt(index);
                }
                else
                {
                    int lastIndex = _items.Count - 1;
                    _items[index] = _items[lastIndex];
                    _items.RemoveAt(lastIndex);
                }
            }
            Dictionary<int, int>  dictionary = new Dictionary<int, int>();
            

            return checksum + _items.Count;
        }

        private int RunSearchById(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                int targetId = GetTargetId(i);
                int foundIndex = -1;

                for (int j = 0; j < _items.Count; j++)
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

                for (int j = 0; j < _items.Count; j++)
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

            for (int i = 0; i < _items.Count; i++)
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

            if (_items.Count == 0)
            {
                return checksum;
            }

            for (int i = 0; i < operations; i++)
            {
                int index = i % _items.Count;
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
            int checksum = _items.Count;
            _items.Clear();

            return checksum;
        }

        private int RunMassFill(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);
            int checksum = 0;

            List<ProjectileData> target;

            if (config.PreallocateCapacity)
            {
                target = new List<ProjectileData>(operationCount);
            }
            else
            {
                target = new List<ProjectileData>();
            }

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                target.Add(item);
                checksum += item.Id;
            }

            _items = target;

            return checksum + _items.Count;
        }

        private int RunEffectArea(BenchmarkConfigData config)
        {
            int checksum = 0;
            int insideCount = 0;

            for (int i = 0; i < _items.Count; i++)
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

            List<ProjectileData> active;

            if (config.PreallocateCapacity)
            {
                active = new List<ProjectileData>(maxActive);
            }
            else
            {
                active = new List<ProjectileData>();
            }

            for (int wave = 0; wave < config.WaveCount; wave++)
            {
                for (int i = 0; i < config.ProjectilesPerWave; i++)
                {
                    ProjectileData item = GetDatasetItem(wave * config.ProjectilesPerWave + i);
                    active.Add(item);
                    checksum += item.Id;
                }

                for (int i = 0; i < active.Count; i++)
                {
                    ProjectileData item = active[i];
                    item.Update(config.DeltaTime);
                    active[i] = item;

                    checksum += Mathf.FloorToInt(item.Position.x + item.Position.y);
                }

                int index = 0;

                while (index < active.Count)
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
                            int lastIndex = active.Count - 1;
                            active[index] = active[lastIndex];
                            active.RemoveAt(lastIndex);
                        }
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            _items = active;

            return checksum + _items.Count;
        }

        private ProjectileData GetDatasetItem(int index)
        {
            if (_dataset == null || _dataset.Count == 0)
            {
                return default;
            }

            return _dataset.Get(index % _dataset.Count);
        }

        private int GetTargetId(int operationIndex)
        {
            if (_dataset == null || _dataset.Count == 0)
            {
                return -1;
            }

            return _dataset.Get(operationIndex % _dataset.Count).Id;
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