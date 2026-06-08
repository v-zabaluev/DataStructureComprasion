using System.Diagnostics;

namespace Benchmark.Core
{
    public static class BenchmarkTimer
    {
        public static void Restart(Stopwatch stopwatch)
        {
            if (stopwatch != null)
            {
                stopwatch.Restart();
            }
        }

        public static void Stop(Stopwatch stopwatch)
        {
            if (stopwatch != null && stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }
        }
    }
}