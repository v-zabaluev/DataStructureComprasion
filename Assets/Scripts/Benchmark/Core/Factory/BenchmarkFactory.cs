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
                default:
                    throw new NotSupportedException(
                        "Benchmark structure is not supported: " + kind);
            }
        }
    }
}