// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 2:10 PM

using System.Collections.Generic;

namespace Mailboxes
{
    public class SimpleMailbox : Mailbox
    {
        readonly Queue<MailboxAction> _actions = new Queue<MailboxAction>(0);

        internal override void QueueAction(in MailboxAction action)
        {
            lock (_actions)
            {
                _actions.Enqueue(action);
            }

            if (!InProgress)
                _dispatcher.Execute(this);
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

        internal override MailboxAction DequeueAction()
        {
            lock (_actions)
            {
                if (_actions.Count > 0)
                {
                    return _actions.Dequeue();
                }
                else
                {
                    return new MailboxAction();
                }
            }
        }
    }
}