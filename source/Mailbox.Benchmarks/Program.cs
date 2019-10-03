using System;
using BenchmarkDotNet.Running;

namespace Mailboxes.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<SimpleMailboxBenchmarks>();
            BenchmarkRunner.Run<ComparisonBenchmarks>();
        }
    }
}
 