using BenchmarkDotNet.Running;

namespace ThirdParty.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<PingPongComparisonBenchmark>();
            BenchmarkRunner.Run<SkynetComparisonBenchmark>();
        }
    }
}
 