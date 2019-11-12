// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 2:10 PM

using System.Collections.Generic;

namespace Mailboxes
{
    public class SimpleMailbox : Mailbox
    {
        readonly Queue<MailboxAction> _actions = new Queue<MailboxAction>(0);

        public SimpleMailbox() { }

        public SimpleMailbox(Dispatcher dispatcher) : base(dispatcher) { }

        protected override void DoQueueAction(in MailboxAction action, object? actionContext)
        {
            lock (_actions)
            {
                _actions.Enqueue(action);
            }
        }

        internal override bool IsEmpty
        {
            get
            {
                lock (_actions)
                {
                    return _actions.Count==0;
                }
            }
        }

        protected override MailboxAction DoDequeueAction()
        {
            lock (_actions)
            {
                return _actions.Count > 0 ? _actions.Dequeue() : new MailboxAction();
            }
        }

        protected internal override void OnStop()
        {
            lock (_actions)
            {
                _actions.Clear();
            }
        }
    }
}