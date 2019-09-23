// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/23/2019 12:24 AM

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Mailboxes.Benchmarks
{
    [MemoryDiagnoser]
    public class SkyNet
    {
        class Actor
        {
            readonly Mailbox _mailbox = new SimpleMailbox();
            readonly long _ordinal;
            public Actor(long ordinal)
            {
                _ordinal = ordinal;
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
        public Task<long> SkyNetOneMillion()
        {
            return new Actor(0).Execute(1000000, 10);
        }
    }
}