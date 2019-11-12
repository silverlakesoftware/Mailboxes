// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 11/8/2019 12:49 AM

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Mailboxes
{
    public static class MailboxExtensions
    {
        public static TaskAwaiterWithContext ContinueWithContext(this Task task, object state)
            => new TaskAwaiterWithContext(task, state);

        public static TaskAwaiterWithContext<TResult> ContinueWithContext<TResult>(this Task<TResult> task, object state)
            => new TaskAwaiterWithContext<TResult>(task, state);

        public readonly struct TaskAwaiterWithContext : INotifyCompletion
        {
            readonly Mailbox _mailbox;
            readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _taskAwaiter;
            readonly object _actionContext;


            public TaskAwaiterWithContext(Task task, object actionContext)
            {
                _mailbox = Mailbox.Current ?? throw new InvalidOperationException("There is no current mailbox for which to set the context.");
                _taskAwaiter = task.ConfigureAwait(false).GetAwaiter();
                _actionContext = actionContext;
            }

            public TaskAwaiterWithContext GetAwaiter() => this;

            public bool IsCompleted => _taskAwaiter.IsCompleted;

            public void OnCompleted(Action continuation)
            {
                // Use the task continuation to do the work of queueing up the real continuation with the context
                // By using .ConfigureAwait(false) above this will usually just continue on the same thread and we can
                // avoid an unnecessary thread switch or queuing to the mailbox just to queue the real work.
                var actionContext = _actionContext;
                var mailbox = _mailbox;
                _taskAwaiter.OnCompleted(() =>
                {
                    mailbox.QueueAction(new MailboxAction(a => (a as Action)?.Invoke(), continuation), actionContext);
                });
            }

            public void GetResult() => _taskAwaiter.GetResult();
        }

        public readonly struct TaskAwaiterWithContext<T> : INotifyCompletion
        {
            readonly Mailbox _mailbox;
            readonly ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter _taskAwaiter;
            readonly object _actionContext;

            public TaskAwaiterWithContext(Task<T> task, object actionContext)
            {
                _mailbox = Mailbox.Current ?? throw new InvalidOperationException("There is no current mailbox for which to set the context.");
                _taskAwaiter = task.ConfigureAwait(false).GetAwaiter();
                _actionContext = actionContext;
            }

            public TaskAwaiterWithContext<T> GetAwaiter() => this;

            public bool IsCompleted => _taskAwaiter.IsCompleted;

            public void OnCompleted(Action continuation)
            {
                // Use the task continuation to do the work of queueing up the real continuation with the context
                // By using .ConfigureAwait(false) above this will usually just continue on the same thread and we can
                // avoid an unnecessary thread switch or queuing to the mailbox just to queue the real work.
                var actionContext = _actionContext;
                var mailbox = _mailbox;
                _taskAwaiter.OnCompleted(() =>
                {
                    mailbox.QueueAction(new MailboxAction(a => (a as Action)?.Invoke(), continuation), actionContext);
                });
            }

            public T GetResult() => _taskAwaiter.GetResult();
        }
    }
}