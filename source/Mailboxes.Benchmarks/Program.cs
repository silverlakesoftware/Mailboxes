// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using BenchmarkDotNet.Running;
using Mailboxes.Benchmarks.ExecutionPatterns;

namespace Mailboxes.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromTypes(new[]
            {
                typeof(CoreMailboxBenchmarks),
                typeof(ComparisonBenchmarks)
            }).Run(args);
        }

        static void RunExecutionPatternBenchmarks()
        {
            BenchmarkRunner.Run<StructBenchmarks>();
            BenchmarkRunner.Run<ClassBenchmarks>();
            BenchmarkRunner.Run<HybridBenchmarks>();
            BenchmarkRunner.Run<Hybrid2Benchmarks>();
            BenchmarkRunner.Run<Hybrid3Benchmarks>();
        }
    }
} 