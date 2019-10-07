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

        public static Dispatcher Default = new ThreadPoolDispatcher();

        public ThreadPoolDispatcher()
        {
            _tlSyncContext= new ThreadLocal<DispatcherSynchronizationContext>(() => new DispatcherSynchronizationContext(this));
        }

        protected internal override void Queue(Mailbox mailbox, SendOrPostCallback d, object state)
        {
//            ThreadPool.QueueUserWorkItem(QueueCallback, new ActionCallback(mailbox, d, state), true);
            var action = new ActionCallback(mailbox, d, state);
            if (mailbox.QueueAction(action))
            {
                Execute(action);
            }
        }

        void QueueCallback(ActionCallback action)
        {
            if (action.Mailbox.QueueAction(action))
            {
                WorkItemCallback(action);
            }
        }

        protected override void Execute(in ActionCallback action)
        {
            if (action.Action==null)
            {
                return;
            }
            ThreadPool.QueueUserWorkItem(WorkItemCallback, action, true);
        }

        public void WorkItemCallback(ActionCallback action)
        {
            var oldSyncContext = SynchronizationContext.Current;
            var syncContext = _tlSyncContext.Value;
            syncContext.SetMailbox(action.Mailbox);
            SynchronizationContext.SetSynchronizationContext(syncContext);
            action.Action(action.State);

            var actions = action.Mailbox.DequeueActions(100);
            foreach (var bulkAction in actions)
            {
                bulkAction.Action(bulkAction.State);
            }

//            for (int i = 0; i < 499; ++i)
//            {
//                action = action.Mailbox.DequeueAction();
//                if (action.Action==null)
//                    break;
//                action.Action(action.State);
//            }

            SynchronizationContext.SetSynchronizationContext(oldSyncContext);
            if (action.Action!=null)
            {
                Execute(action.Mailbox.DequeueAction());
            }
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
                _dispatcher.Queue(_mailbox, d, state);
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotImplementedException();
            }
        }
    }
}