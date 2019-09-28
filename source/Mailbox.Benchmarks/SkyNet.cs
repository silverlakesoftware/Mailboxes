// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/23/2019 12:24 AM

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Mailboxes.Benchmarks
{
    [MemoryDiagnoser]
    public class SkyNet
    {
        struct Actor
        {
            readonly Mailbox _mailbox;
            readonly long _ordinal;

            public Actor(long ordinal)
            {
                _ordinal = ordinal;
                _mailbox = new SimpleMailbox();
            }

            public async Task<long> Execute(long size, long div)
            {
                await _mailbox;
                if (size == 1)
                    return _ordinal;
                var tasks = new List<Task<long>>((int)div);
                for (long i = 0; i < div; ++i)
                {
                    var childOrdinal = _ordinal + i * (size / div);
                    var actor = new Actor(childOrdinal);
                    tasks.Add(actor.Execute(size / div, div));
                }

                await Task.WhenAll(tasks);
                return tasks.Sum(t => t.Result);
            }
        }

        [Benchmark]
        public Task<long> SkynetMailboxes()
        {
            return new Actor(0).Execute(1000000, 10);
        }

        [Benchmark]
        public long SkynetParallel() => SkyNet.skynetParallel(0, 1000000, 10);

        [Benchmark]
        public Task<long> SkynetValueTask() => SkyNet.skynetThreadpoolValueTaskAsync(0, 1000000, 10);

        private static Task<long> skynetThreadpoolValueTaskAsync(long num, long size, long div)
        {
            if (size == 1)
            {
                return Task.FromResult(num);
            }
            else
            {
                var tasks = new List<Task<long>>((int)div);
                for (var i = 0; i < div; i++)
                {
                    var sub_num = num + i * (size / div);
                    var task = Task.Run(() => skynetValueTaskAsync(sub_num, size / div, div).AsTask());
                    tasks.Add(task);
                }
                return Task.WhenAll(tasks).ContinueWith(skynetAggregator);
            }
        }

        static long skynetAggregator(Task<long[]> children)
        {
            long sumAsync = 0;
            var results = children.Result;
            for (var i = 0; i < results.Length; i++)
            {
                sumAsync += results[i];
            }
            return sumAsync;
        }


        private static ValueTask<long> skynetValueTaskAsync(long num, long size, long div)
        {
            if (size == 1)
            {
                return new ValueTask<long>(num);
            }
            else
            {
                long subtotal = 0;
                List<Task<long>> tasks = null;

                for (var i = 0; i < div; i++)
                {
                    var sub_num = num + i * (size / div);
                    var task = skynetValueTaskAsync(sub_num, size / div, div);
                    if (task.IsCompleted)
                    {
                        subtotal += task.Result;
                    }
                    else
                    {
                        if (tasks == null)
                        {
                            tasks = new List<Task<long>>((int)div);
                        }
                        tasks.Add(task.AsTask());
                    }
                }

                if (tasks == null)
                {
                    return new ValueTask<long>(subtotal);
                }
                else if (subtotal > 0)
                {
                    tasks.Add(Task.FromResult(subtotal));
                }
                return new ValueTask<long>((Task.WhenAll(tasks).ContinueWith(skynetAggregator)));
            }
        }

        private static long skynetParallel(long num, long size, long div)
        {
            if (size == 1)
            {
                return num;
            }
            else
            {
                long total = 0;

                long[] source = new long[div];
                for (var i = 0; i < div; i++)
                {
                    source[i] = i;
                }

                var rangePartitioner = Partitioner.Create(0L, source.Length);

                Parallel.ForEach(rangePartitioner,
                    () => 0L,
                    (range, loopState, runningtotal) =>
                    {
                        for (long i = range.Item1; i < range.Item2; i++)
                        {
                            var sub_num = num + i * (size / div);
                            runningtotal += skynetSync(sub_num, size / div, div);
                        }
                        return runningtotal;
                    },
                    (subtotal) => Interlocked.Add(ref total, subtotal)
                );

                return total;
            }
        }
        private static long skynetSync(long num, long size, long div)
        {
            if (size == 1)
            {
                return num;
            }
            else
            {
                long sum = 0;
                for (var i = 0; i < div; i++)
                {
                    var sub_num = num + i * (size / div);
                    sum += skynetSync(sub_num, size / div, div);
                }
                return sum;
            }
        }
    }
}