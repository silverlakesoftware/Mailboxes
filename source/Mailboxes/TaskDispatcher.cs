// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 1:59 PM

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mailboxes
{
    public class TaskDispatcher : Dispatcher
    {
        readonly ThreadLocal<DispatcherSynchronizationContext> _tlSyncContext;
        // TODO: Put locking here in the dispatch use this for in progress

        public static Dispatcher Default = new TaskDispatcher();

        public TaskDispatcher()
        {
            _tlSyncContext= new ThreadLocal<DispatcherSynchronizationContext>(() => new DispatcherSynchronizationContext(this));
        }

//        protected internal override void Queue(ActionCallback ac)
//        {
//            Queue(ac);
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
            lock (mailbox)
            {
                mailbox.Task = mailbox.Task.ContinueWith(_ => QueueCallback(action), TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        void QueueCallback(ActionCallback action)
        {
            bool execute = false;
            if (action.Mailbox.InProgress)
                action.Mailbox.QueueAction(action);
            else
            {
                action.Mailbox.InProgress = true;
                execute = true;
            }

            if (execute)
            {
                Execute(action);
            }
        }

        void DequeueNextAction(object? mailboxObj)
        {
            var mailbox = (Mailbox)mailboxObj!;
            if (mailbox.IsEmpty)
            {
                mailbox.InProgress = false;
                return;
            }

            var action = mailbox.DequeueAction();

            if (mailbox.IsEmpty)
            {
                Execute(action);
                return;
            }

            var actions = new List<ActionCallback>(100);
            while (!mailbox.IsEmpty && actions.Count < 100)
                actions.Add(mailbox.DequeueAction());

            ThreadPool.QueueUserWorkItem(WorkItemCallback, actions.ToArray(), true);
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

//            bool runAgain = true;
//            for (int i = 0; i < 200 && runAgain; ++i)
//            {
//                lock (action.Mailbox)
//                {
//                    if (!action.Mailbox.IsEmpty)
//                    {
//                        action = action.Mailbox.DequeueAction();
//                        runAgain = true;
//                    }
//                    else
//                    {
//                        runAgain = false;
//                    }
//                }
//
//                if (runAgain)
//                {
//                    action.Action(action.State);
//                }
//            }


//            int count = 0;
//            while (count<10)
//            {
//                int actionCount = 0;
//                ActionCallback[] actions;
//
//                lock (action.Mailbox)
//                {
//                    if (action.Mailbox.IsEmpty)
//                        break;
//                    
//                    actionCount = ((SimpleMailbox)action.Mailbox).DequeueActions(out actions);
//                }
//
//                for(int i = 0; i<actionCount; ++i)
//                {
//                    actions[i].Action(actions[i].State);
//                }
//
//                ++count;
////                syncContext.DispatchActions();
//            }

            SynchronizationContext.SetSynchronizationContext(oldSyncContext);
//            syncContext.DispatchActions();

            lock (action.Mailbox)
            {
                action.Mailbox.Task.ContinueWith((_, mailbox) => DequeueNextAction(mailbox), action.Mailbox);
            }
        }

        public void WorkItemCallback(ActionCallback[] actions)
        {
            var oldSyncContext = SynchronizationContext.Current;
            var syncContext = _tlSyncContext.Value;
            syncContext.SetMailbox(actions[0].Mailbox);
            SynchronizationContext.SetSynchronizationContext(syncContext);

            for (int i = 0; i < actions.Length; ++i)
                actions[i].Action(actions[i].State);

            SynchronizationContext.SetSynchronizationContext(oldSyncContext);

            lock (actions[0].Mailbox)
            {
                actions[0].Mailbox.Task.ContinueWith((_, mailbox) => DequeueNextAction(mailbox), actions[0].Mailbox);
            }
        }
        class DispatcherSynchronizationContext : SynchronizationContext
        {
            readonly Dispatcher _dispatcher;
            Mailbox _mailbox;
            List<ActionCallback> _actions = new List<ActionCallback>();
            AsyncLocal<Mailbox> _alMailbox = new AsyncLocal<Mailbox>();

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
                (_dispatcher as TaskDispatcher).QueueBulk(_actions);
                _actions = new List<ActionCallback>();
            }

            public override void Post(SendOrPostCallback d, object? state)
            {
                _dispatcher.Queue(_mailbox, d, state); 
                //_dispatcher.Queue(_mailbox, Execute, new Callback(d,state));
            }

            readonly struct Callback
            {
                public Callback(SendOrPostCallback action, object? state)
                {
                    Action = action;
                    State = state;
                }

                public SendOrPostCallback Action { get; }
                public object? State { get; }

            }

            void Execute(object? callbackObj)
            {
                var callback = (Callback)callbackObj!;
                SynchronizationContext.SetSynchronizationContext(null);
                callback.Action(callback.State);
                SynchronizationContext.SetSynchronizationContext(this);
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotImplementedException();
            }
        }
    }
}