// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

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