// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 10/30/2019 11:31 PM

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mailboxes.Tests
{
    public class DefaultSchedulerTests
    {
        const long TimerRange = 15;

        public Scheduler CreateScheduler() => new DefaultScheduler();

        [Fact]
        public void ImmediateScheduleDoesNotChangeNow()
        {
            var sut = CreateScheduler();
            var start = sut.Now;
            DateTimeOffset fired = DateTimeOffset.MaxValue;
            sut.Schedule<object?>(0, null, _ => fired = sut.Now);
            var offset = fired - start;
            Assert.True(offset.TotalMilliseconds <= 1);
        }

        [Fact]
        public async Task NoDelayDoesNotChangeNow()
        {
            var sut = CreateScheduler();
            var start = sut.Now;
            await Task.Delay(0);
            var fired = sut.Now;
            var offset = fired - start;
            Assert.True(offset.TotalMilliseconds <= 1);
        }

        [Fact]
        public void ScheduleForOneMillisecondOccursWhenExpected()
        {
            var sut = CreateScheduler();
            var start = sut.Now;
            DateTimeOffset fired = DateTimeOffset.MaxValue;
            using var mre = new ManualResetEventSlim();
            var threadId = Thread.CurrentThread.ManagedThreadId;
            sut.Schedule<object?>(1, null, _ =>
            {
                Assert.NotEqual(threadId, Thread.CurrentThread.ManagedThreadId);
                fired = sut.Now;
                mre.Set();
            });
            mre.Wait();
            var offset = fired - start;
            Assert.True(offset.TotalMilliseconds > 1 && offset.TotalMilliseconds < (1 + TimerRange));
        }

        [Fact]
        public async Task DelayForOneMillisecondOccursWhenExpected()
        {
            var sut = CreateScheduler();
            var start = sut.Now;
            var threadId = Thread.CurrentThread.ManagedThreadId;
            await sut.Delay(1).ConfigureAwait(false);
            Assert.NotEqual(threadId, Thread.CurrentThread.ManagedThreadId);
            var fired = sut.Now;
            var offset = fired - start;
            Assert.True(offset.TotalMilliseconds > 1 && offset.TotalMilliseconds < (1 + TimerRange));
        }

        [Fact]
        public async Task CancelPreventsActionFromFiring()
        {
            var sut = CreateScheduler();
            using var cts = new CancellationTokenSource();
            var actionCalled = false;
            sut.Schedule<object?>(100, null, _ => actionCalled = true, cts.Token);
            cts.Cancel();
            await Task.Delay((int)(2 + TimerRange));
            Assert.False(actionCalled);
        }

        [Fact]
        public Task CancelCancelsDelay()
        {
            var sut = CreateScheduler();
            using var cts = new CancellationTokenSource();
            var task = sut.Delay(100, cts.Token);
            cts.Cancel();
            return Assert.ThrowsAsync<TaskCanceledException>(() => task);
        }
    }
}