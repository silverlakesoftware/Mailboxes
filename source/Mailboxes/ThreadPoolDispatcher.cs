// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 1:59 PM

using System;
using System.Threading;

namespace Mailboxes
{
    public class ThreadPoolDispatcher : Dispatcher
    {
        readonly ThreadLocal<DispatcherSynchronizationContext> _tlSyncContext;
        public static readonly Dispatcher Default = new ThreadPoolDispatcher();

        public ThreadPoolDispatcher()
        {
            _tlSyncContext= new ThreadLocal<DispatcherSynchronizationContext>(() => new DispatcherSynchronizationContext(this));
        }

        public override void Execute(Mailbox mailbox)
        {
            ThreadPool.QueueUserWorkItem(m => (m.Dispatcher as ThreadPoolDispatcher).WorkItemCallback(m), mailbox, true);
        }

        public void WorkItemCallback(Mailbox mailbox)
        {
            var action = mailbox.DequeueAction();
            if (action.Action==null)
            {
                mailbox.TryContinueRunning();
                return;
            }

            var oldSyncContext = SynchronizationContext.Current;
            var syncContext = _tlSyncContext.Value;
            syncContext.SetMailbox(mailbox);
            SynchronizationContext.SetSynchronizationContext(syncContext);

            action.Action(action.State);

            for (int i = 0; i < 99; ++i)
            {
                action = mailbox.DequeueAction();
                if (action.Action!=null)
                {
                    action.Action(action.State);
                }
                else
                {
                    break;
                }
            }

            SynchronizationContext.SetSynchronizationContext(oldSyncContext);
            mailbox.TryContinueRunning();
        }

        class DispatcherSynchronizationContext : SynchronizationContext
        {
            readonly Dispatcher _dispatcher;
            Mailbox _mailbox;

            public DispatcherSynchronizationContext(Dispatcher dispatcher)
            {
                _dispatcher = dispatcher;
            }

            public void SetMailbox(Mailbox mailbox) => _mailbox = mailbox;

            public override void Post(SendOrPostCallback d, object? state)
            {
                _mailbox.QueueAction(new MailboxAction(d, state));
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotImplementedException();
            }
        }
    }
}