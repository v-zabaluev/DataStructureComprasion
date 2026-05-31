namespace Benchmark.Core.Enums
{
    public enum BenchmarkScenario
    {
        SequentialIteration = 0,
        AddElements = 1,
        RemoveElement = 2,
        SearchById = 3,
        ContainsElement = 4,
        UpdateAll = 5,
        UpdateOne = 6,
        ClearCollection = 7,
        MassFill = 8,
        EffectArea = 9,
        GroupBySector = 10,
        EcsMassUpdateWithJobsBurst = 11,
        ParallelWriteResults = 12,
        FullWaveCycle = 13
    }
}