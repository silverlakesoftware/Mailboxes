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
        static OldMailbox CreateMailbox() => new OldSimpleMailbox();

    //        [Benchmark]
    //        public Mailbox Create() => new SimpleMailbox();

        [Benchmark]
        public Task<int> CreateAndOneCall()
        {
            return Test(CreateMailbox());

            static async Task<int> Test(OldMailbox mailbox)
            {
                await mailbox;
                return 42;
            }
        }

//        [Benchmark]
//        public Task<int> CreateAndOneDirectCall()
//        {
//            var mailbox = CreateMailbox();
//            var tcs = new TaskCompletionSource<int>();
//
//            mailbox.Execute(() => tcs.SetResult(42));
//            return tcs.Task;
//        }

        [Benchmark]
        public Task<int> Increment()
        {
            var mailbox = CreateMailbox();
            int value = 0;

            var tcs = new TaskCompletionSource<int>();

            Parallel.For(0, 1000, DoIncrement);

            return tcs.Task;

            async void DoIncrement(int _)
            {
                await mailbox;
                ++value;
                if (value==1000)
                {
                    tcs.SetResult(value);
                }
            }
        }

        [Benchmark]
        public Task<int[]> ParallelIncrement()
        {
            var mailbox1 = CreateMailbox();
            var mailbox2 = CreateMailbox();
            int value1 = 0;
            int value2 = 0;

            var tcs1 = new TaskCompletionSource<int>();
            var tcs2 = new TaskCompletionSource<int>();

            Parallel.For(0, 1000, i =>
            {
                if (i % 2==0)
                    DoIncrement1(i);
                else
                    DoIncrement2(i);
            });

            return Task.WhenAll(tcs1.Task, tcs2.Task);

            async void DoIncrement1(int i)
            {
                await mailbox1;
                ++value1;
                if (value1 == 500)
                {
                    tcs1.SetResult(value1);
                }
            }

            async void DoIncrement2(int i)
            {
                await mailbox2;
                ++value2;
                if (value2 == 500)
                {
                    tcs2.SetResult(value2);
                }
            }
        }
    }
}