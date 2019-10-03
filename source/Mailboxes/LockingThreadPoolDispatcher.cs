// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 1:59 PM

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mailboxes
{
    public class LockingThreadPoolDispatcher : Dispatcher
    {
        readonly ThreadLocal<DispatcherSynchronizationContext> _tlSyncContext;
        // TODO: Put locking here in the dispatch use this for in progress

        public static Dispatcher Default = new LockingThreadPoolDispatcher();

        public LockingThreadPoolDispatcher()
        {
            _tlSyncContext= new ThreadLocal<DispatcherSynchronizationContext>(() => new DispatcherSynchronizationContext(this));
        }

//        protected internal override void Queue(ActionCallback ac)
//        {
//            Queue(ac)
//        }

        void QueueBulk(List<ActionCallback> actions)
        {
            foreach (var mailboxActions in actions.GroupBy(a => a.Mailbox))
            {
                var mailbox = mailboxActions.Key;
                var actionsArray = mailboxActions.ToArray();
                ActionCallback nextAction = new ActionCallback();
                bool hasLock = Monitor.TryEnter(mailbox);
                try
                {
                    if (!hasLock)
                    {
                        Task.Run(() => QueueMailboxActions(mailbox, actionsArray));
                        return;
                    }

                    foreach (var action in mailboxActions)
                        mailbox.QueueAction(action);
                    if (!mailbox.InProgress)
                    {
                        nextAction = mailbox.DequeueAction();
                        mailbox.InProgress = true;
                    }
                }
                finally
                {
                    if (hasLock)
                        Monitor.Exit(mailbox);
                }
                if (nextAction.Action != null)
                    Execute(nextAction);

                //                if (actionsArray.Length > 99)
                //                {
                //                    Task.Run(() => { QueueMailboxActions(mailbox, actionsArray); });
                //                }
                //                else
                //                {
                //                    QueueMailboxActions(mailbox, actionsArray);
                //                }
            }

            void QueueMailboxActions(Mailbox mailbox, ActionCallback[] mailboxActions)
            {
                ActionCallback nextAction = new ActionCallback();
                lock (mailbox)
                {
                    foreach (var action in mailboxActions)
                        mailbox.QueueAction(action);
                    if (!mailbox.InProgress)
                    {
                        nextAction = mailbox.DequeueAction();
                        mailbox.InProgress = true;
                    }
                }

                if (nextAction.Mailbox!=null)
                    Execute(nextAction);
            }
        }

        protected internal override void Queue(Mailbox mailbox, SendOrPostCallback d, object state)
        {
            var action = new ActionCallback(mailbox, d, state);

//            if (SynchronizationContext.Current is DispatcherSynchronizationContext context)
//            {
//                if (context.Mailbox==mailbox)
//                    mailbox.QueueAction(action);
//                else
//                    context.InnerQueue(action);
//                return;
//            }
            
//            if (SynchronizationContext.Current is DispatcherSynchronizationContext context && context.Mailbox==mailbox)
//            {
//                mailbox.QueueAction(action);
//                return;
//            }


//                        ThreadPool.QueueUserWorkItem(QueueCallback, new ActionCallback(mailbox, d, state), true);
            bool execute = false;

            bool hasLock = Monitor.TryEnter(mailbox,2);
            try
            {
                if (!hasLock)
                {
                    ThreadPool.QueueUserWorkItem(QueueCallback, action, true);
                    return;
                }

                if (mailbox.InProgress)
                    mailbox.QueueAction(action);
                else
                {
                    mailbox.InProgress = true;
                    execute = true;
                }
            }
            finally
            {
                if (hasLock)
                    Monitor.Exit(mailbox);
            }

//            lock (mailbox)
//            {
//                if (mailbox.InProgress)
//                    mailbox.QueueAction(action);
//                else
//                {
//                    mailbox.InProgress = true;
//                    execute = true;
//                }
//            }

            if (execute)
                Execute(action);
        }

        void QueueCallback(ActionCallback action)
        {
            bool execute = false;
            lock (action.Mailbox)
            {
                if (action.Mailbox.InProgress)
                    action.Mailbox.QueueAction(action);
                else
                {
                    action.Mailbox.InProgress = true;
                    execute = true;
                }
            }

            if (execute)
                Execute(action);
        }

        protected override void Execute(ActionCallback action)
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

            int count = 0;
            if (!action.Mailbox.IsEmpty && count<10)
            {
                List<ActionCallback> actions;

                lock (action.Mailbox)
                {
                    actions = action.Mailbox.DequeueActions(20);
                }

                foreach (var bulkAction in actions)
                {
                    bulkAction.Action(bulkAction.State);
                }

                ++count;
//                syncContext.DispatchActions();
            }

            SynchronizationContext.SetSynchronizationContext(oldSyncContext);
//            syncContext.DispatchActions();

            ActionCallback nextAction = new ActionCallback();
            lock (action.Mailbox)
            {
                if (!action.Mailbox.IsEmpty)
                {
                    nextAction = action.Mailbox.DequeueAction();
                }
                else
                {
                    action.Mailbox.InProgress = false;
                }
            }

            if (nextAction.Action!=null)
            {
                Execute(nextAction);
            }
        }

        class DispatcherSynchronizationContext : SynchronizationContext
        {
            readonly Dispatcher _dispatcher;
            Mailbox _mailbox;
            List<ActionCallback> _actions = new List<ActionCallback>();

            public DispatcherSynchronizationContext(Dispatcher dispatcher)
            {
                _dispatcher = dispatcher;
            }

            public Mailbox Mailbox => _mailbox;

            public void SetMailbox(Mailbox mailbox) => _mailbox = mailbox;

            public void InnerQueue(ActionCallback action)
            {
                _actions.Add(action);
            }

            public void DispatchActions()
            {
                if (_actions.Count == 0)
                    return;
                (_dispatcher as LockingThreadPoolDispatcher).QueueBulk(_actions);
                _actions = new List<ActionCallback>();
            }

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