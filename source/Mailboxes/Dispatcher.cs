// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 2:00 PM

namespace Mailboxes
{
    public abstract class Dispatcher
    {
        public abstract void Execute(Mailbox mailbox);

        public abstract void Execute(in MailboxAction action);
    }
}