using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mailboxes.Tests
{
    public abstract class MailboxTests
    {
        protected abstract Mailbox CreateMailbox();

        [Fact]
        public async Task MailboxIsNotReentrant()
        {
            int check = 0;
            var mailbox = CreateMailbox();

            using var mre1 = new ManualResetEventSlim();
            var tcs1 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tcs2 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = Task.Run(() => Test(tcs1));
            _ = Task.Run(() => Test(tcs2));
            mre1.Set();

            void Test(TaskCompletionSource<bool> tcs)
            {
                mailbox.Execute(() =>
                {
                    var result = Interlocked.Increment(ref check);
                    Assert.Equal(1, result);
                    mre1.Wait();
                    Interlocked.Decrement(ref check);
                    tcs.SetResult(true);
                });
            }

            await Task.WhenAll(tcs1.Task, tcs2.Task);
        }

        [Fact]
        public async Task MailboxIsNotReentrantUnderStress()
        {
            int check = 0;
            var mailbox = CreateMailbox();

            const int concurrency = 10000;
            var tasks = new Task[concurrency];

            Parallel.For(0, concurrency, i =>
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                tasks[i] = tcs.Task;

                Test(tcs);
            });

            void Test(TaskCompletionSource<bool> tcs)
            {
                mailbox.Execute(() =>
                {
                    var result = Interlocked.Increment(ref check);
                    Assert.Equal(1, result);
                    Thread.SpinWait(5);
                    Interlocked.Decrement(ref check);
                    tcs.SetResult(true);
                });
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task CancellationWaitsForMessage()
        {
            var mailbox = CreateMailbox();

            using var cts = new CancellationTokenSource();
            using var mre1 = new ManualResetEventSlim();
            using var mre2 = new ManualResetEventSlim();

            var test = Test(cts.Token);
            mre2.Wait();
            cts.Cancel();
            Assert.True(cts.Token.IsCancellationRequested);
            mre1.Set();
            await test;

            async Task Test(CancellationToken ct)
            {
                await mailbox.Include(ref ct);
                mre2.Set();
                mre1.Wait();
                Assert.False(ct.IsCancellationRequested);
                await Task.Run(() =>
                {
                    while (!ct.IsCancellationRequested)
                        Thread.Sleep(1);
                });
                Assert.True(ct.IsCancellationRequested);
            }
        }
    }
}
