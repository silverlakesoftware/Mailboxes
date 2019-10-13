// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 3:41 PM

using System.Collections.Concurrent;

namespace Mailboxes
{
    public class ConcurrentMailbox : Mailbox
    {
        readonly ConcurrentQueue<MailboxAction> _actions = new ConcurrentQueue<MailboxAction>();

        internal override void QueueAction(in MailboxAction action)
        {
            _actions.Enqueue(action);

            if (!InProgress)
                _dispatcher.Execute(this);
        }

        internal override bool IsEmpty => _actions.IsEmpty;

        internal override MailboxAction DequeueAction()
        {
            return _actions.TryDequeue(out var result) ? result : new MailboxAction();
        }
    }
}