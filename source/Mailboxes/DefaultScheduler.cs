// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 10/27/2019 12:52 AM

using System;
using System.Threading;

namespace Mailboxes
{
    public class DefaultScheduler : Scheduler
    {
        public override DateTimeOffset Now => DateTimeOffset.Now;

        protected override void DoSchedule<TState>(long timeSpanMs, TState state, Action<TState> action, CancellationToken ct = default)
        {
            _ = new TimerAction<TState>(timeSpanMs, state, action, ct);
        }

        abstract class TimerAction
        {
            internal static void ExecuteCallback(object? o) => ((TimerAction)o!).Execute();

            protected internal abstract void Execute();
        }

        sealed class TimerAction<TState> : TimerAction, IDisposable
        {
            public TimerAction(long timeSpanMs, TState state, Action<TState> action, CancellationToken cancellationToken)
            {
                State = state;
                Action = action;
                CancellationToken = cancellationToken;
                Timer = new Timer(o => ExecuteCallback(o), this, timeSpanMs, Timeout.Infinite);
                cancellationToken.Register(o => (o as Timer)?.Dispose(), Timer);
            }

            Timer Timer { get; }

            TState State { get; }

            Action<TState> Action { get; }

            CancellationToken CancellationToken { get; }

            protected internal override void Execute()
            {
                try
                {
                    if (!CancellationToken.IsCancellationRequested)
                    {
                        Action(State);
                    }
                }
                finally
                {
                    Dispose();
                }
            }

            public void Dispose()
            {
                Timer?.Dispose();
            }
        }
    }
}