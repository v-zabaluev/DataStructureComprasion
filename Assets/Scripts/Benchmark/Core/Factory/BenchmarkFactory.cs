using System;
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

                    break;
                default:
                    throw new NotSupportedException(
                        "Benchmark structure is not supported: " + kind);
            }
        }
    }
}