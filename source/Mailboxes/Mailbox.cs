// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 2:05 PM

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mailboxes
{
    public abstract class Mailbox
    {
        readonly MailboxAwaiter _awaiter;
        volatile int _inProgress;
        protected Dispatcher _dispatcher;

        protected Mailbox()
        {
            _awaiter = new MailboxAwaiter(this);
            _dispatcher = ThreadPoolDispatcher.Default;
        }

        public Dispatcher Dispatcher => _dispatcher;

        public bool InProgress
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _inProgress==1; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool SetInProgress(bool inProgress) => Interlocked.Exchange(ref _inProgress, inProgress ? 1 : 0) == 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(Action action)
        {
            QueueAction(new MailboxAction(a => (a as Action)?.Invoke(), action));
        }

        public ref readonly MailboxAwaiter GetAwaiter() => ref _awaiter;

        internal abstract void QueueAction(in MailboxAction action);

        internal virtual bool IsEmpty => false;

        internal abstract MailboxAction DequeueAction();

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
                _mailbox.QueueAction(new MailboxAction(a => (a as Action)?.Invoke(), continuation));
            }

            public void GetResult() { }
        }
    }
}