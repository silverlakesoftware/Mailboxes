using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mailboxes.Tests
{
    public class SimpleMailboxTests
    {
        OldMailbox GetMailbox() => new OldSimpleMailbox();

        [Fact]
        public async Task CancellationWaitsForMessage()
        {
            var mailbox = GetMailbox();

            using var cts = new CancellationTokenSource();
            using var mre = new ManualResetEventSlim();

            var test = Test(cts.Token);
            cts.Cancel();
            Assert.True(cts.Token.IsCancellationRequested);
            mre.Set();
            await test;

            async Task Test(CancellationToken ct)
            {
                await mailbox.Include(ref ct);
                mre.Wait();
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
