// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/23/2019 12:03 AM

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Mailboxes.Benchmarks
{
    [MemoryDiagnoser]
    public class SimpleMailboxBenchmarks
    {
        [Benchmark]
        public Mailbox Create() => new SimpleMailbox();

        [Benchmark]
        public ValueTask<int> CreateAndOneCall()
        {
            return Test(new SimpleMailbox());

            static async ValueTask<int> Test(Mailbox mailbox)
            {
                await mailbox;
                return 42;
            }
        }

        [Benchmark]
        public async Task<int> Increment()
        {
            var mailbox = new SimpleMailbox();
            int value = 0;

            Parallel.For(0, 1000, _ => DoIncrement());

            await mailbox;
            return value;

            async void DoIncrement()
            {
                await mailbox;
                ++value;
            }
        }
    }
}