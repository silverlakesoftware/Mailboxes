using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mailboxes;

namespace MiniActors
{
    class Program
    {
        static readonly Mailbox _mailbox = new ConcurrentMailbox();

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var mailbox = new ConcurrentMailbox();

            await TestA();

            var cts = new CancellationTokenSource(100);
            await TestC(cts.Token);

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

        public static async Task TestC( CancellationToken ct )
        {
            await _mailbox.Include(ref ct);
            var sw = Stopwatch.StartNew();
            await DoDelay(ct);
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        public static async Task DoDelay(CancellationToken ct)
        {
            await Task.Delay(5000, ct);
        }
    }
}
