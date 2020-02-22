// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Mailboxes.Benchmarks.PingPong;

namespace Mailboxes.Benchmarks
{
    [MemoryDiagnoser]
    public class ComparisonBenchmarks
    {
        [Benchmark]
        public Task<long> SkyNet()
        {
            return new Skynet.Mailboxes.RootActor().Run();
        }

        [Benchmark]
        public Task<long> SkyNetAwait()
        {
            return new Skynet.Mailboxes.RootActorAwait().Run();
        }

        [Arguments(50)]
        [Arguments(100)]
        [Arguments(500)]
        [Benchmark]
        public void PingPong(int executionUnits)
        {
            int messageCount = 1000000;
            int batchSize = 100;

            var oldExecutionUnits = ThreadPoolDispatcher.Default.ExecutionUnits;
            ThreadPoolDispatcher.Default.ExecutionUnits = executionUnits;
            var clientCount = Environment.ProcessorCount * 2;
            var clients = new MailboxPingActor[clientCount];
            var echos = new MalboxEchoActor[clientCount];
            var completions = new TaskCompletionSource<bool>[clientCount];

            for (var i = 0; i < clientCount; i++)
            {
                var tsc = new TaskCompletionSource<bool>();
                completions[i] = tsc;

                var pingActor = new MailboxPingActor(tsc, messageCount, batchSize);
                var echoActor = new MalboxEchoActor();

                clients[i] = pingActor;
                echos[i] = echoActor;
            }

            var tasks = completions.Select(tsc => tsc.Task).ToArray();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < clientCount; i++)
            {
                var client = clients[i];
                var echo = echos[i];

                client.Start(echo);
            }

            Task.WaitAll(tasks);

            sw.Stop();
            var totalMessages = messageCount * 2 * clientCount;

            var x = (int)(totalMessages / (double)sw.ElapsedMilliseconds * 1000.0d);
            Console.WriteLine($"{x} msg/sec");
            ThreadPoolDispatcher.Default.ExecutionUnits = oldExecutionUnits;
        }

        [Arguments(50)]
        [Arguments(100)]
        [Arguments(500)]
        [Benchmark]
        public void PingPongAwait(int executionUnits)
        {
            int messageCount = 1000000;
            int batchSize = 100;

            var oldExecutionUnits = ThreadPoolDispatcher.Default.ExecutionUnits;
            ThreadPoolDispatcher.Default.ExecutionUnits = executionUnits;
            var clientCount = Environment.ProcessorCount * 2;
            var clients = new MailboxPingActorAwait[clientCount];
            var echos = new MalboxEchoActorAwait[clientCount];
            var completions = new TaskCompletionSource<bool>[clientCount];

            for (var i = 0; i < clientCount; i++)
            {
                var tsc = new TaskCompletionSource<bool>();
                completions[i] = tsc;

                var pingActor = new MailboxPingActorAwait(tsc, messageCount, batchSize);
                var echoActor = new MalboxEchoActorAwait();

                clients[i] = pingActor;
                echos[i] = echoActor;
            }

            var tasks = completions.Select(tsc => tsc.Task).ToArray();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < clientCount; i++)
            {
                var client = clients[i];
                var echo = echos[i];

                _ = client.Start(echo);
            }

            Task.WaitAll(tasks);

            sw.Stop();
            var totalMessages = messageCount * 2 * clientCount;

            var x = (int)(totalMessages / (double)sw.ElapsedMilliseconds * 1000.0d);
            Console.WriteLine($"{x} msg/sec");
            ThreadPoolDispatcher.Default.ExecutionUnits = oldExecutionUnits;
        }
    }
}