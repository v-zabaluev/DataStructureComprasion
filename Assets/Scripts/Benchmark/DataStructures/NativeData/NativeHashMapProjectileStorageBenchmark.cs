using System.Diagnostics;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.Data;
using Projectile.Data;
using Unity.Collections;
using UnityEngine;

namespace Benchmark.Benchmarks
{
    public class NativeHashMapProjectileStorageBenchmark : IProjectileStorageBenchmark
    {
        private NativeHashMap<int, ProjectileData> _items;
        private ProjectileDataset _dataset;
        private bool _isCreated;

        public string StructureName => "NativeHashMap";

        public bool IsScenarioSupported(BenchmarkScenario scenario)
        {
            return scenario != BenchmarkScenario.JobsBurstMassUpdate &&
                   scenario != BenchmarkScenario.ParallelWriteResults;
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
            NativeArray<int> keys = _items.GetKeyArray(Allocator.Temp);

            for (int i = 0; i < keys.Length; i++)
            {
                int key = keys[i];
                ProjectileData item = _items[key];

                item.Update(config.DeltaTime);
                _items[key] = item;

                checksum += Mathf.FloorToInt(item.Position.x);
                checksum += Mathf.FloorToInt(item.Position.y);
            }

            keys.Dispose();
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

            for (int wave = 0; wave < config.WaveCount; wave++)
            {
                for (int i = 0; i < config.ProjectilesPerWave; i++)
                {
                    ProjectileData item = GetDatasetItem(wave * config.ProjectilesPerWave + i);
                    active[item.Id] = item;
                    checksum += item.Id;
                }

                NativeArray<int> keysForUpdate = active.GetKeyArray(Allocator.Temp);

                for (int i = 0; i < keysForUpdate.Length; i++)
                {
                    int key = keysForUpdate[i];
                    ProjectileData item = active[key];

                    item.Update(config.DeltaTime);
                    active[key] = item;

                    checksum += Mathf.FloorToInt(item.Position.x + item.Position.y);
                }

                keysForUpdate.Dispose();

                NativeArray<int> keysForRemove = active.GetKeyArray(Allocator.Temp);

                for (int i = 0; i < keysForRemove.Length; i++)
                {
                    int key = keysForRemove[i];

                    if (active.TryGetValue(key, out ProjectileData item) && item.IsExpired())
                    {
                        checksum += item.Id;
                        active.Remove(key);
                    }
                }

                keysForRemove.Dispose();
            }

            DisposeCurrentMap();
            _items = active;
            _isCreated = true;

            return checksum + _items.Count;
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
    }
}
