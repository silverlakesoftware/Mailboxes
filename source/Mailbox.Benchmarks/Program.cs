using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace Mailboxes.Benchmarks
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<SimpleMailboxBenchmarks>();

            BenchmarkRunner.Run<SkyNet>();

            //Console.WriteLine(await new SkyNet().SkyNetOneMillion());
        }
    }
}
