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
        private ProjectileDataset _dataset;
        private bool _isCreated;
        private bool _isLookupDataCreated;

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

            if (config.Scenario == BenchmarkScenario.AddElements)
            {
                return;
            }

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
            int remaining = _items.Count;

            for (int i = 0; i < operations; i++)
            {
                if (remaining == 0)
                {
                    break;
                }

                int targetId = GetTargetId(i);

                if (_items.TryGetValue(targetId, out ProjectileData item))
                {
                    checksum += item.Id;
                    _items.Remove(targetId);
                    remaining--;
                }
            }

            return checksum + remaining;
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