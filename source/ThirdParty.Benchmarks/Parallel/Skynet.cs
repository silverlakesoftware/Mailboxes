// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 1:35 AM

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace ThirdParty.Benchmarks.Parallel
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Skynet
    {
        internal static long skynetParallel(long num, long size, long div)
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

                System.Threading.Tasks.Parallel.ForEach(rangePartitioner,
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