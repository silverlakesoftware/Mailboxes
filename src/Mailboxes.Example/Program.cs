// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mailboxes.Example
{
    class Program
    {
        static readonly Mailbox _mailbox = new ConcurrentMailbox();

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            await TestA();

            //var cts = new CancellationTokenSource(100);
            //await TestC(cts.Token);

            await TestD();

            Console.WriteLine(SynchronizationContext.Current);
        }

        public static async Task TestA()
        {
            await _mailbox;
            Console.WriteLine("Section A1");
            //Console.WriteLine(_mailbox.QueueDepth);
            Console.WriteLine(SynchronizationContext.Current);
            var httpClient = new HttpClient();
            var t1 = httpClient.GetStringAsync("https://google.com").ContinueWith(t => Console.WriteLine("In ContinueWith"));
            var t2 = Task.Delay(500);

            TestB();
            TestB();

            await t1;
            Console.WriteLine("Section A2");
            //Console.WriteLine(_mailbox.QueueDepth);
            Console.WriteLine(SynchronizationContext.Current);
            await t2;
            Console.WriteLine("Section A3");
            //Console.WriteLine(_mailbox.QueueDepth);
            Console.WriteLine(SynchronizationContext.Current);
        }

        public static async void TestB()
        {
            await _mailbox;
            Console.WriteLine("Section B1");
            //Console.WriteLine(_mailbox.QueueDepth);
            Console.WriteLine(SynchronizationContext.Current);
        }

        public static async Task TestC(CancellationToken ct)
        {
            await _mailbox.Include(ref ct);
            var sw = Stopwatch.StartNew();
            await DoDelay(ct);
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        public static async Task<int> DoDelay(CancellationToken ct)
        {
            await Task.Delay(5000, ct);
            return 1;
        }

        public static async Task TestD()
        {
            var state = new object();
            await _mailbox.WithContext(state);
            var state2 = new object();
            await DoDelay(CancellationToken.None).ContinueWithContext(state2);
            // Need to explore if setting the state on the synccontext in OnCompleted is safe, when it's not used
            // until after execution in the Post
        }
    }
}