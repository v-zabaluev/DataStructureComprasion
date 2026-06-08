using System.Collections.Generic;
using System.Diagnostics;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile.Data;
using UnityEngine;

namespace Benchmark.Benchmarks
{
    public class DictionaryProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private Dictionary<int, ProjectileData> _items;
        private ProjectileDataset _dataset;

        public string StructureName => "Dictionary";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return true;
        }

        public void Prepare(BenchmarkConfigData config, ProjectileDataset dataset)
        {
            _dataset = dataset;

            int length = dataset == null ? 0 : dataset.Count;

            if (config.PreallocateCapacity)
            {
                _items = new Dictionary<int, ProjectileData>(length);
            }
            else
            {
                _items = new Dictionary<int, ProjectileData>();
            }

            for (int i = 0; i < length; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                _items[item.Id] = item;
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
                    checksum = RunBatchIdLookup(config);
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
            _items = null;
            _dataset = null;
        }

        private int RunSequentialIteration()
        {
            int checksum = 0;

            foreach (KeyValuePair<int, ProjectileData> pair in _items)
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

            Dictionary<int, ProjectileData> target;

            if (config.PreallocateCapacity)
            {
                target = new Dictionary<int, ProjectileData>(operationCount);
            }
            else
            {
                target = new Dictionary<int, ProjectileData>();
            }

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                target[item.Id] = item;
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

            if (_items.Count == 0)
            {
                return checksum;
            }

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

            Dictionary<int, ProjectileData> target;

            if (config.PreallocateCapacity)
            {
                target = new Dictionary<int, ProjectileData>(operationCount);
            }
            else
            {
                target = new Dictionary<int, ProjectileData>();
            }

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                target[item.Id] = item;
                checksum += item.Id;
            }

            _items = target;

            return checksum + _items.Count;
        }

        private int RunEffectArea(BenchmarkConfigData config)
        {
            int checksum = 0;
            int insideCount = 0;

            foreach (KeyValuePair<int, ProjectileData> pair in _items)
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

            Dictionary<int, ProjectileData> active = config.PreallocateCapacity
                ? new Dictionary<int, ProjectileData>(maxActive)
                : new Dictionary<int, ProjectileData>();

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

            _items = active;

            return checksum + _items.Count;
        }

        private int RunBatchIdLookup(BenchmarkConfigData config)
        {
            int operations = GetSafeOperationCount(config);
            int checksum = 0;

            for (int i = 0; i < operations; i++)
            {
                int targetId = GetTargetId(i);

                if (_items.ContainsKey(targetId))
                {
                    checksum += targetId;
                }
            }

            return checksum;
        }

        private int RunJobStructureBuild(BenchmarkConfigData config)
        {
            int operationCount = GetSafeOperationCount(config);
            int checksum = 0;

            Dictionary<int, ProjectileData> target = config.PreallocateCapacity
                ? new Dictionary<int, ProjectileData>(operationCount)
                : new Dictionary<int, ProjectileData>();

            for (int i = 0; i < operationCount; i++)
            {
                ProjectileData item = GetDatasetItem(i);
                target[item.Id] = item;
                checksum += item.Id;
            }

            _items = target;

            return checksum + _items.Count;
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