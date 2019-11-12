// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 3:41 PM

using System.Collections.Concurrent;

namespace Mailboxes
{
    public class ConcurrentMailbox : Mailbox
    {
        ConcurrentQueue<MailboxAction>? _actions = new ConcurrentQueue<MailboxAction>();

        public ConcurrentMailbox() { }

        public ConcurrentMailbox(Dispatcher? dispatcher) : base(dispatcher) { }

        protected override void DoQueueAction(in MailboxAction action, object? actionContext)
        {
            _actions?.Enqueue(action);
        }

        internal override bool IsEmpty => _actions?.IsEmpty ?? true;

        protected override MailboxAction DoDequeueAction()
        {
            var actions = _actions;
            return actions!=null && actions.TryDequeue(out var result) ? result : new MailboxAction();
        }

        protected internal override void OnStop()
        {
            _actions = null;
        }
    }
}