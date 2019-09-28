using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace Mailboxes.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<SimpleMailboxBenchmarks>();
            //BenchmarkRunner.Run<SkyNet>();
        }
    }
}
