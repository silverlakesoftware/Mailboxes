// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/21/2019 5:32 PM

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mailboxes
{
    public abstract class OldMailbox
    {
        readonly MailboxAwaiter _awaiter;

        protected OldMailbox()
        {
            _awaiter = new MailboxAwaiter(this);
        }

        protected abstract void Execute(ActionCallback action);

        public void Execute(Action action)
        {
            Execute(new ActionCallback(DoExecute, action));
            static void DoExecute(object c) => ((Action)c)();
        }

        public abstract int QueueDepth { get; }


        public ref readonly MailboxAwaiter GetAwaiter() => ref _awaiter;
        
        public OldMailbox Include(ref CancellationToken ct)
        {
            var cts = new CancellationTokenSource();
            ct.Register(() =>
            {
                Execute(new ActionCallback(DoCancel,cts));
            });
            ct = cts.Token;
            return this;

            static void DoCancel(object state) => ((CancellationTokenSource)state).Cancel();
        }

        public readonly struct MailboxAwaiter : INotifyCompletion
        {
            readonly OldMailbox _mailbox;

            public MailboxAwaiter(OldMailbox mailbox)
            {
                _mailbox = mailbox;
            }

            public bool IsCompleted => false;

            public void OnCompleted(Action continuation)
            {
                _mailbox.Execute(new ActionCallback(Execute, continuation));

                static void Execute(object c) => ((Action)c)();
            }

            public void GetResult() { }
        }

        public readonly struct ActionCallback
        {
            public ActionCallback(SendOrPostCallback action, object state)
            {
                Action = action;
                State = state;
            }

            public SendOrPostCallback Action { get; }
            public object State { get; }
        }
    }
}