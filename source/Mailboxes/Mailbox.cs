// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/21/2019 5:32 PM

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mailboxes
{
    public abstract class Mailbox
    {
        readonly MailboxAwaiter _awaiter;

        protected Mailbox()
        {
            _awaiter = new MailboxAwaiter(this);
        }

        protected abstract void Execute(Action action);

        public abstract int QueueDepth { get; }


        public ref readonly MailboxAwaiter GetAwaiter() => ref _awaiter;
        
        public Mailbox Include(ref CancellationToken ct)
        {
            var cts = new CancellationTokenSource();
            ct.Register(() =>
            {
                Execute(cts.Cancel);
            });
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
                _mailbox.Execute(continuation);
            }

            public void GetResult() { }
        }
    }
}