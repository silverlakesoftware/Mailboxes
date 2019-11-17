// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 2:05 PM

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mailboxes
{
    public abstract class Mailbox
    {
        const int StopStateNo = 0;
        const int StopStateYes = 1;

        const int RunStateIdle = 0;
        const int RunStateRunning = 1;

        readonly MailboxAwaiter _awaiter;

        volatile int _stopState = StopStateNo;
        volatile int _runState = RunStateIdle;
        protected Dispatcher _dispatcher;
        TaskCompletionSource<bool>? _stopTcs;

        protected Mailbox() : this(null) { }

        protected Mailbox(Dispatcher? dispatcher)
        {
            _awaiter = new MailboxAwaiter(this);
            _dispatcher = dispatcher ?? ThreadPoolDispatcher.Default;
            SyncContext = new MailboxSynchronizationContext(this);
        }

        internal MailboxSynchronizationContext SyncContext { get; }

        internal static Mailbox? Current => (SynchronizationContext.Current as MailboxSynchronizationContext)?.Mailbox;

        public Dispatcher Dispatcher => _dispatcher;

        public bool IsStopped => _stopState==StopStateYes;

        public bool IsRunning => _runState==RunStateRunning;

        public IEventHandler EventHandler { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Action action, object? actionContext = null)
        {
            QueueAction(new MailboxAction(a => ((Action)a!).Invoke(), action), actionContext);
        }

        public Task ExecuteAsync(Func<Task> action, object? actionContext = null)
        {
            var tcs = new TaskCompletionSource<VoidResult>();
            QueueAction(new MailboxAction(a => ExecuteAsyncAction((Func<Task>)a!, tcs), action), actionContext);
            return tcs.Task;
        }

        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, object? actionContext = null)
        {
            var tcs = new TaskCompletionSource<T>();
            QueueAction(new MailboxAction(a => ExecuteAsyncAction((Func<Task<T>>)a!, tcs), action), actionContext);
            return tcs.Task;
        }

        void ExecuteAsyncAction(Func<Task> action, TaskCompletionSource<VoidResult> tcs)
        {
            action().ContinueWith(t =>
            {
                switch (t.Status)
                {
                    case TaskStatus.Canceled:
                        tcs.SetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        tcs.SetException(t.Exception!.InnerException!);
                        break;
                    case TaskStatus.RanToCompletion:
                        tcs.SetResult(new VoidResult());
                        break;
                }
            });
        }

        void ExecuteAsyncAction<T>(Func<Task<T>> action, TaskCompletionSource<T> tcs)
        {
            action().ContinueWith(t =>
            {
                switch (t.Status)
                {
                    case TaskStatus.Canceled:
                        tcs.SetCanceled();
                        break;
                    case TaskStatus.Faulted:
                        tcs.SetException(t.Exception!.InnerException!);
                        break;
                    case TaskStatus.RanToCompletion:
                        tcs.SetResult(t.Result);
                        break;
                }
            });
        }

        public ref readonly MailboxAwaiter GetAwaiter() => ref _awaiter;

        public MailboxAwaiterWithState WithContext(object actionContext) => new MailboxAwaiterWithState(this, actionContext);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void QueueAction(in MailboxAction action, object? actionContext)
        {
            // If the mailbox is stopped, ignore new actions.  In the future we'll probably have an event to
            // trigger for Actor support.
            if (_stopState==StopStateYes)
            {
                return;
            }

            DoQueueAction(action, actionContext);

            // If we're idle, let's dispatch an action.  We have to check the Status after the item is queued
            // to play nice with TryContinueRunning.
            if (_runState==RunStateIdle)
            {
                TryStartRunning();
            }
        }

        protected abstract void DoQueueAction(in MailboxAction action, object? actionContext);

        internal abstract bool IsEmpty { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal MailboxAction DequeueAction() => _stopState==StopStateYes ? new MailboxAction() : DoDequeueAction();

        protected abstract MailboxAction DoDequeueAction();

        public Task StopAsync()
        {
            var runState = _runState;

            if (Interlocked.Exchange(ref _stopState, StopStateYes)==StopStateYes)
            {
                return Task.CompletedTask;
            }

            _stopTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _stopTcs.Task.ContinueWith(_ => OnStop());

            if (runState==RunStateIdle)
            {
                Stopped();
                return Task.CompletedTask;
            }

            return _stopTcs.Task;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void TryStartRunning()
        {
            if (Interlocked.Exchange(ref _runState, RunStateRunning)==RunStateRunning)
            {
                return;
            }

            Dispatcher.Execute(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TryContinueRunning()
        {
            if (!IsEmpty && !IsStopped)
            {
                Dispatcher.Execute(this);
            }
            else
            {
                // If we're already idle, we don't need to do anything
                if (Interlocked.Exchange(ref _runState, RunStateIdle)==RunStateIdle)
                {
                    return;
                }

                // We've transitioned to stopped which can only happen once so make notifications
                if (IsStopped)
                {
                    Stopped();
                    return;
                }

                // There's work left to do, queue it up
                if (!IsEmpty)
                {
                    TryStartRunning();
                }
            }
        }

        /// <summary>
        /// Called from a <see cref="Dispatcher"/> when an unhandled exception occurs when processing an action.
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <returns>true to continue (i.e. call <see cref="TryContinueRunning"/>, false if the mailbox is stopped.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void HandleException(Exception ex)
        {
            EventHandler?.OnException(ex);
        }

        void Stopped()
        {
            // The problem is that an item can be queued and it will transition to running and then transition back to idle
            // after Stopped has already been executed.  TrySetResult works here

            SpinWait.SpinUntil(() => _stopTcs!=null);
            Debug.Assert(_stopTcs!=null, nameof(_stopTcs) + " != null");
            _stopTcs.TrySetResult(true);
        }

        protected internal abstract void OnStop();

        public Mailbox Include(ref CancellationToken ct, object? actionState = null)
        {
            var cts = new CancellationTokenSource();
            ct.Register(() => QueueAction(new MailboxAction(state => ((CancellationTokenSource)state!).Cancel(), cts), actionState));
            ct = cts.Token;
            return this;
        }

        public readonly struct MailboxAwaiter : INotifyCompletion
        {
            readonly Mailbox _mailbox;

            public MailboxAwaiter(Mailbox mailbox)
            {
                _mailbox = mailbox;
            }

            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                _mailbox.QueueAction(new MailboxAction(a => ((Action)a!).Invoke(), continuation), null);
            }

            public void GetResult() { }
        }

        public readonly struct MailboxAwaiterWithState : INotifyCompletion
        {
            readonly Mailbox _mailbox;
            readonly object _actionContext;

            public MailboxAwaiterWithState(Mailbox mailbox, object actionContext)
            {
                _mailbox = mailbox;
                _actionContext = actionContext;
            }

            public MailboxAwaiterWithState GetAwaiter() => this;

            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                _mailbox.QueueAction(new MailboxAction(a => ((Action)a!).Invoke(), continuation), _actionContext);
            }

            public void GetResult() { }
        }

        public class MailboxSynchronizationContext : SynchronizationContext
        {
            // ReSharper disable once FieldCanBeMadeReadOnly.Local
            Mailbox _mailbox;

            public MailboxSynchronizationContext(Mailbox mailbox) => _mailbox = mailbox;

            internal Mailbox Mailbox => _mailbox;

            public override SynchronizationContext CreateCopy() => this;

            public override void Post(SendOrPostCallback d, object? state)
            {
                _mailbox.QueueAction(new MailboxAction(d, state), null);
            }

            public override void Send(SendOrPostCallback d, object? state) => throw new NotImplementedException();
        }

        private struct VoidResult { };


        public interface IEventHandler
        {
            void OnException(Exception ex);
        }
    }
}