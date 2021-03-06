﻿// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;
using Akka.Actor;
using BenchmarkDotNet.Attributes;
using Proto;
using ThirdParty.Benchmarks.AkkaDotNet;
using AkkaProps = Akka.Actor.Props;
using ProtoProps = Proto.Props;

namespace ThirdParty.Benchmarks
{
    [MemoryDiagnoser]
    public class SkynetComparisonBenchmark
    {
        [Benchmark]
        public long Parallel() => global::ThirdParty.Benchmarks.Parallel.Skynet.skynetParallel(0, 10000, 10);

        [Benchmark]
        public Task<long> ValueTask() => global::ThirdParty.Benchmarks.ValueTask.Skynet.skynetThreadpoolValueTaskAsync(0, 10000, 10);

        [Benchmark]
        public Task AkkaDotNet()
        {
            var system = ActorSystem.Create("main");
            var root = system.ActorOf(AkkaProps.Create<RootActor>());
            var run = new RootActor.Run();
            return root.Ask(run);
        }

        [Benchmark]
        public Task Proto()
        {
            var context = new RootContext();
            var props = ProtoProps.FromProducer(() => new Proto.RootActor());
            var pid = context.Spawn(props);

            var run = new Proto.RootActor.Run();

            return context.RequestAsync<Proto.RootActor.Result>(pid, run);
        }
    }
}