// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 1:59 PM

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mailboxes
{
    public class LockingThreadPoolDispatcher : Dispatcher
    {
        readonly ThreadLocal<DispatcherSynchronizationContext> _tlSyncContext;
        public static readonly Dispatcher Default = new LockingThreadPoolDispatcher();

        public LockingThreadPoolDispatcher()
        {
            _tlSyncContext= new ThreadLocal<DispatcherSynchronizationContext>(() => new DispatcherSynchronizationContext(this));
        }

        public override void Execute(Mailbox mailbox)
        {
            if (mailbox.SetInProgress(true))
                return;
            ContinueExecution(mailbox);
        }

        public override void Execute(in MailboxAction action)
        {
            ThreadPool.QueueUserWorkItem(WorkItemCallback, action, true);
        }

        public void WorkItemCallback(MailboxAction action)
        {
            var mailbox = action.Mailbox;
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
            ContinueExecution(mailbox);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ContinueExecution(Mailbox mailbox)
        {
            if (!mailbox.IsEmpty)
            {
                var action = mailbox.DequeueAction();
                Execute(action);
            }
            else
            {
                if (!mailbox.SetInProgress(false))
                    return;
                if (!mailbox.IsEmpty)
                    Execute(mailbox);
            }

//            var action = mailbox.DequeueAction();
//            if (action.Action != null)
//            {
//                Execute(action);
//            }
//            else
//            {
//                if (!mailbox.SetInProgress(false))
//                    return;
//                if (!mailbox.IsEmpty)
//                    Execute(mailbox);
//            }
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
                _mailbox.QueueAction(new MailboxAction(_mailbox, d, state));
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotImplementedException();
            }
        }
    }
}