// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 2:05 PM

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mailboxes
{
    public abstract class Mailbox
    {
        readonly MailboxAwaiter _awaiter;
        protected Dispatcher _dispatcher;

        protected Mailbox()
        {
            _awaiter = new MailboxAwaiter(this);
            _dispatcher = LockingThreadPoolDispatcher.Default;
            Task = Task.CompletedTask;
        }


        public void Execute(Action action)
        {
            _dispatcher.Queue(this, Execute, action);
            static void Execute(object a) => ((Action)a)();
        }

        public bool InProgress { get; set; }

        public Task Task { get; set;}

        public ref readonly MailboxAwaiter GetAwaiter() => ref _awaiter;

        internal abstract bool QueueAction(in ActionCallback action);

        internal virtual bool IsEmpty => false;

        internal abstract ActionCallback DequeueAction();

        internal virtual List<ActionCallback> DequeueActions(int max) => new List<ActionCallback>(Enumerable.Repeat(DequeueAction(), 1));

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
                _mailbox._dispatcher.Queue(_mailbox, Execute, continuation);
                static void Execute(object c) => ((Action)c)();
            }

            public void GetResult() { }
        }
    }
}