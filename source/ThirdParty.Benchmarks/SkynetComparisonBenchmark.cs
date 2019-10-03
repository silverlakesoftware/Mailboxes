// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/28/2019 9:46 PM

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
            var run = new RootActor.Run(1);
            return root.Ask(run);
        }

        [Benchmark]
        public Task Proto()
        {
            var context = new RootContext();
            var props = ProtoProps.FromProducer(() => new Proto.RootActor());
            var pid = context.Spawn(props);

            var run = new Proto.RootActor.Run(1);

            return context.RequestAsync<Proto.RootActor.Result>(pid, run);
        }
    }
}