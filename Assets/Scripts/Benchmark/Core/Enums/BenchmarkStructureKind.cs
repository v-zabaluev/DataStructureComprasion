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

        NativeArrayJob = 8,
        NativeListJob = 9,
        NativeHashMapJob = 10,
        NativeHashSetJob = 11,

        Dummy = 15,
    }
}