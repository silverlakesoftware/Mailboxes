// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 10/27/2019 12:37 AM

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mailboxes
{
    public abstract class Scheduler
    {
        public abstract DateTimeOffset Now { get; }

        public void Schedule<TState>(TimeSpan dueTime, TState state, Action<TState> action, CancellationToken ct = default) =>
            Schedule((long)dueTime.TotalMilliseconds, state, action, ct);

        public void Schedule<TState>(long timeSpanMs, TState state, Action<TState> action, CancellationToken ct = default)
        {
            if (timeSpanMs < 0 || ct.IsCancellationRequested)
            {
                return;
            }

            if (timeSpanMs == 0)
            {
                action(state);
                return;
            }

            DoSchedule(timeSpanMs, state, action, ct);
        }

        protected abstract void DoSchedule<TState>(long timeSpanMs, TState state, Action<TState> action, CancellationToken ct = default);

        public Task Delay(TimeSpan timeSpan, CancellationToken ct = default) => Delay((long)timeSpan.TotalMilliseconds, ct);

        public Task Delay(long timeSpanMs, CancellationToken ct = default)
        {
            if (timeSpanMs <= 0)
            {
                return Task.CompletedTask;
            }

            if (ct.IsCancellationRequested)
            {
                return Task.FromCanceled(ct);
            }

            var taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            ct.Register(tcs => (tcs as TaskCompletionSource<bool>)!.TrySetCanceled(ct), taskCompletionSource);
            DoSchedule(timeSpanMs, taskCompletionSource, tcs => tcs.TrySetResult(true), ct);
            return taskCompletionSource.Task;
        }
    }
}