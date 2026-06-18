using System;
using Benchmark.Benchmarks;
using Benchmark.Core.Enums;
using Benchmark.Core.Interfaces;
using Benchmark.DataStructures;
using UnityEngine;

namespace Benchmark.UI
{
    public class BenchmarkFactory : MonoBehaviour
    {
        public IProjectileStorageBenchmark Create(BenchmarkStructureKind kind)
        {
            switch (kind)
            {
                case BenchmarkStructureKind.Dummy:
                    return new DummyBenchmark();
                case BenchmarkStructureKind.Array:
                    return new ArrayProjectileStorageBenchmark();
                case BenchmarkStructureKind.List:
                    return new ListProjectileStorageBenchmark();
                case BenchmarkStructureKind.Dictionary:
                    return new DictionaryProjectileStorageBenchmark();
                case BenchmarkStructureKind.HashSet:
                    return new HashSetProjectileStorageBenchmark();
                case BenchmarkStructureKind.NativeArray:
                    return new NativeArrayProjectileStorageBenchmark();
                case BenchmarkStructureKind.NativeList:
                    return new NativeListProjectileStorageBenchmark();
                case BenchmarkStructureKind.NativeHashSet:
                    return new NativeHashSetProjectileStorageBenchmark();
                case BenchmarkStructureKind.NativeHashMap:
                    return new NativeHashMapProjectileStorageBenchmark();
                case BenchmarkStructureKind.NativeArrayJob:
                    return new NativeArrayJobProjectileStorageBenchmark();
                case BenchmarkStructureKind.NativeListJob:
                    return new NativeListJobProjectileStorageBenchmark();
                case BenchmarkStructureKind.NativeHashMapJob:
                    return new NativeHashMapJobProjectileStorageBenchmark();
                case BenchmarkStructureKind.NativeHashSetJob:
                    return new NativeHashSetJobProjectileStorageBenchmark();
                default:
                    throw new NotSupportedException(
                        "Benchmark structure is not supported: " + kind);
            }
        }
    }
}
