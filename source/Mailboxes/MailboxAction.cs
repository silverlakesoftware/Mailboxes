// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 2:08 PM

using System.Threading;

namespace Mailboxes
{
    public struct MailboxAction
    {
        public MailboxAction(Mailbox mailbox, SendOrPostCallback action, object state)
        {
            Mailbox = mailbox;
            Action = action;
            State = state;
        }

        public Mailbox Mailbox { get; }

        public SendOrPostCallback Action { get; }

        public object State { get; }
    }
}