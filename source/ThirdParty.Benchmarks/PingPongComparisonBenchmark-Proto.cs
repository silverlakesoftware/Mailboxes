// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 10:59 AM

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Proto;
using Proto.Mailbox;
using ThirdParty.Benchmarks.Proto;

namespace ThirdParty.Benchmarks
{
    public partial class PingPongComparisonBenchmark
    {
        [Benchmark]
        [Arguments(100)]
        [Arguments(200)]
        [Arguments(500)]
        public void Proto(int throughput)
        {
            int messageCount = 1000000;
            int batchSize = 100;
            const bool useBoundedMailbox = true; 
            var d = new ThreadPoolDispatcher { Throughput = throughput };

            var context = new RootContext();
            var clientCount = Environment.ProcessorCount * 2;
            var clients = new PID[clientCount];
            var echos = new PID[clientCount];
            var completions = new TaskCompletionSource<bool>[clientCount];


            var echoProps = Props.FromProducer(() => new EchoActor())
                .WithDispatcher(d)
                .WithMailbox(() => useBoundedMailbox ? BoundedMailbox.Create(2048) : UnboundedMailbox.Create());

            for (var i = 0; i < clientCount; i++)
            {
                var tsc = new TaskCompletionSource<bool>();
                completions[i] = tsc;
                var clientProps = Props.FromProducer(() => new PingActor(tsc, messageCount, batchSize))
                    .WithDispatcher(d)
                    .WithMailbox(() => useBoundedMailbox ? BoundedMailbox.Create(2048) : UnboundedMailbox.Create());

                clients[i] = context.Spawn(clientProps);
                echos[i] = context.Spawn(echoProps);
            }
            var tasks = completions.Select(tsc => tsc.Task).ToArray();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < clientCount; i++)
            {
                var client = clients[i];
                var echo = echos[i];

                context.Send(client, new Start(echo));
            }
            Task.WaitAll(tasks);

            sw.Stop();
            var totalMessages = messageCount * 2 * clientCount;

            var x = (int)(totalMessages / (double)sw.ElapsedMilliseconds * 1000.0d);
            Console.WriteLine($"{x} msg/sec");
        }
    }
}