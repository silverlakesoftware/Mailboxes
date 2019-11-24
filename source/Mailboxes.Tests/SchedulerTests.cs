// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

// Created by Jamie da Silva on 10/30/2019 11:02 PM

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mailboxes.Tests
{
    public class SchedulerTests
    {
        [Fact]
        public void NegativeTimeSpanReturnsImmediatelyNoAction()
        {
            var sut = (Scheduler)new FakeScheduler();
            bool didRun = false;
            sut.Schedule<object?>(-1, null, _ => didRun = true);
            Assert.False(didRun);
            Assert.False(((FakeScheduler)sut).DoScheduleCalled);
        }

        [Fact]
        public void CancelledTokenReturnsImmediatelyNoAction()
        {
            var sut = (Scheduler)new FakeScheduler();
            bool didRun = false;
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            sut.Schedule<object?>(1, null, _ => didRun = true, cts.Token);
            Assert.False(didRun);
            Assert.False(((FakeScheduler)sut).DoScheduleCalled);
        }

        [Fact]
        public void ZeroTimeSpanRunsActionImmediately()
        {
            var sut = (Scheduler)new FakeScheduler();
            bool didRun = false;
            sut.Schedule<object?>(0, null, _ => didRun = true);
            Assert.True(didRun);
            Assert.False(((FakeScheduler)sut).DoScheduleCalled);
        }

        [Fact]
        public void ActionIsScheduled()
        {
            var sut = (Scheduler)new FakeScheduler();
            sut.Schedule<object?>(1, null, _ => { });
            Assert.True(((FakeScheduler)sut).DoScheduleCalled);
        }

        [Fact]
        public void TimeSpanOverloadIsScheduled()
        {
            var sut = (Scheduler)new FakeScheduler();
            sut.Schedule<object?>(TimeSpan.FromMilliseconds(2), null, _ => { });
            Assert.True(((FakeScheduler)sut).DoScheduleCalled);
            Assert.Equal(2, ((FakeScheduler)sut).TimeSpanMs);
        }

        [Fact]
        public void CancelledTokenDelayReturnsImmediately()
        {
            var sut = (Scheduler)new FakeScheduler();
            var stopWatch = Stopwatch.StartNew();
            var threadId = Thread.CurrentThread.ManagedThreadId;
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var delayTask = sut.Delay(1, cts.Token);
            Assert.True(delayTask.IsCanceled);
        }

        [Fact]
        public async Task NegativeOrZeroTimeSpanDelayReturnsImmediately()
        {
            var sut = (Scheduler)new FakeScheduler();
            await sut.Delay(-1);
            await sut.Delay(0);
            Assert.False(((FakeScheduler)sut).DoScheduleCalled);
        }

        [Fact]
        public async Task DelayIsScheduled()
        {
            var sut = (Scheduler)new FakeScheduler();
            await sut.Delay(1);
            Assert.True(((FakeScheduler)sut).DoScheduleCalled);
        }

        [Fact]
        public async Task TimeSpanDelayOverloadIsScheduled()
        {
            var sut = (Scheduler)new FakeScheduler();
            await sut.Delay(TimeSpan.FromMilliseconds(2));
            Assert.True(((FakeScheduler)sut).DoScheduleCalled);
            Assert.Equal(2, ((FakeScheduler)sut).TimeSpanMs);
        }

        class FakeScheduler : Scheduler
        {
            public override DateTimeOffset Now => throw new NotImplementedException();

            protected override void DoSchedule<TState>(long timeSpanMs, TState state, Action<TState> action, CancellationToken ct = default)
            {
                TimeSpanMs = timeSpanMs;
                DoScheduleCalled = true;
                action(state);
            }

            public bool DoScheduleCalled { get; private set; }

            public long TimeSpanMs { get; private set; }
        }
    }
}