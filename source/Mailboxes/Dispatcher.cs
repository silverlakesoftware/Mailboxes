// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 2:00 PM

using System.Threading;

namespace Mailboxes
{
    public abstract class Dispatcher
    {
        protected internal virtual void Queue(ActionCallback ac) => Queue(ac.Mailbox, ac.Action, ac.State);

        protected internal abstract void Queue(Mailbox mailbox, SendOrPostCallback d, object? state);
    
        protected abstract void Execute(ActionCallback action);
    }
}