// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 2:08 PM

using System.Threading;

namespace Mailboxes
{
    public readonly struct MailboxAction
    {
        public MailboxAction(SendOrPostCallback action, object? state)
        {
            Action = action;
            State = state;
        }

        public SendOrPostCallback Action { get; }

        public object? State { get; }
    }
}