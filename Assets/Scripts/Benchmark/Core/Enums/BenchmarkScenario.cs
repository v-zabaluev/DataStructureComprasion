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
        EcsMassUpdateWithJobsBurst = 10,
        FullWaveCycle = 11
    }
}