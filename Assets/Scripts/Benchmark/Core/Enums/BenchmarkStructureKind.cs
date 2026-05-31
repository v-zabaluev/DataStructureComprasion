namespace Benchmark.Core.Enums
{
    public enum BenchmarkStructureKind
    {
        Array = 0,
        List = 1,
        Dictionary = 2,
        HashSet = 3,

        NativeArray = 4,
        NativeList = 5,
        NativeHashSet = 6,
        NativeHashMap = 7,
        NativeParallelHashMap = 8,
        NativeParallelMultiHashMap = 9,

        DynamicBuffer = 10,
        Dummy = 11,
    }
}