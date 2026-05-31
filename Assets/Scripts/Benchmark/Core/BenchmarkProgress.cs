namespace Benchmark.Core
{
    public struct BenchmarkProgress
    {
        public readonly string Status;
        public readonly int CurrentStep;
        public readonly int TotalSteps;

        public BenchmarkProgress(string status, int currentStep, int totalSteps)
        {
            Status = status;
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
        }
    }
}