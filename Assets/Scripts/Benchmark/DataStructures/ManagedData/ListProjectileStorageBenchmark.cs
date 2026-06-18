using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            return true;
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
                _items.Add(GetDatasetItem(i));
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