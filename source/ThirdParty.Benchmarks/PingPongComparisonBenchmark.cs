// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 10:59 AM

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Util.Internal;
using BenchmarkDotNet.Attributes;
using ThirdParty.Benchmarks.AkkaDotNet;

namespace ThirdParty.Benchmarks
{
    [MemoryDiagnoser]
    public class PingPongComparisonBenchmark
    {
        [Benchmark]
        [Arguments(100)]
        [Arguments(200)]
        [Arguments(500)]
        public void AkkaDotNet( int throughput )
        {
            var mainSystem = ActorSystem.Create("main");

            int messageCount = 1000000;
            int batchSize = 100;
//            var dispatcherType = "custom-dispatcher";
//            var mailboxType = "bounded-mailbox";

            var clientCount = Environment.ProcessorCount * 2;
            var clients = new IActorRef[clientCount];
            var echos = new IActorRef[clientCount];
            var completions = new TaskCompletionSource<bool>[clientCount];

            var echoProps = Props.Create(typeof(EchoActor));
//                    .WithDispatcher(dispatcherType)
//                    .WithMailbox(mailboxType);

            for (var i = 0; i < clientCount; i++)
            {
                var tsc = new TaskCompletionSource<bool>();
                completions[i] = tsc;

                var clientProps = Props.Create(() => new PingActor(tsc, messageCount, batchSize));
//                        .WithDispatcher(dispatcherType)
//                        .WithMailbox(mailboxType);

                var clientLocalActorRef = (RepointableActorRef)mainSystem.ActorOf(clientProps);
                SpinWait.SpinUntil(() => clientLocalActorRef.IsStarted);
                clientLocalActorRef.Underlying.AsInstanceOf<ActorCell>().Dispatcher.Throughput = throughput;

                var echoLocalActorRef = (RepointableActorRef)mainSystem.ActorOf(echoProps);
                SpinWait.SpinUntil(() => echoLocalActorRef.IsStarted);
                echoLocalActorRef.Underlying.AsInstanceOf<ActorCell>().Dispatcher.Throughput = throughput;

                clients[i] = clientLocalActorRef;
                echos[i] = echoLocalActorRef;
            }

            var tasks = completions.Select(tsc => tsc.Task).ToArray();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < clientCount; i++)
            {
                var client = clients[i];
                var echo = echos[i];

                client.Tell(new Start(echo));
            }

            Task.WaitAll(tasks);

            sw.Stop();
            var totalMessages = messageCount * 2 * clientCount;

            var x = (int)(totalMessages / (double)sw.ElapsedMilliseconds * 1000.0d);
            Console.WriteLine($"{x} msg/sec");
        }
    }
}