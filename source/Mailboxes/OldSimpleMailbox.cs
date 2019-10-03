// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/21/2019 10:54 AM

using System;
using System.Collections.Generic;
using System.Threading;

namespace Mailboxes
{
    public class OldSimpleMailbox : OldMailbox
    {
        bool _inProgress;
        readonly Queue<ActionCallback> _actions = new Queue<ActionCallback>(0);

        readonly static ThreadLocal<MailboxSynchronizationContext> _tlSyncContext = new ThreadLocal<MailboxSynchronizationContext>(() => new MailboxSynchronizationContext(null));
        //readonly MailboxSynchronizationContext _syncContext;

        public OldSimpleMailbox()
        {
            //_syncContext = new MailboxSynchronizationContext(this);
        }

        public override int QueueDepth => _actions.Count;

        protected override void Execute(ActionCallback action)
        {
            lock (_actions)
            {
                if (_inProgress)
                {
                    _actions.Enqueue(action);
                    return;
                }

                _inProgress = true;
            }

            ThreadPool.QueueUserWorkItem(RunAction, action, true);
        }

        void RunAction(ActionCallback action)
        {
//            if (!(actionObject is ActionCallback action))
//                return;
            var oldSyncContext = SynchronizationContext.Current;
            //SynchronizationContext.SetSynchronizationContext(_syncContext);
            var syncContext = _tlSyncContext.Value;
            syncContext.SetMailbox(this);
            SynchronizationContext.SetSynchronizationContext(syncContext);
            action.Action(action.State);

            bool runAgain = true;
            for (int i = 0; i < 100 && runAgain; ++i)
            {
                lock (_actions)
                {
                    if (_actions.Count > 0)
                    {
                        action = _actions.Dequeue();
                        runAgain = true;
                    }
                    else
                    {
                        runAgain = false;
                    }
                }

                if (runAgain)
                {
                    action.Action(action.State);
                }
            }

            SynchronizationContext.SetSynchronizationContext(oldSyncContext);

            QueueNextWork();
        }

        void RunBatch(List<ActionCallback> actions)
        {
            //            if (!(actionObject is ActionCallback action))
            //                return;
            var oldSyncContext = SynchronizationContext.Current;
            //SynchronizationContext.SetSynchronizationContext(_syncContext);
            var syncContext = _tlSyncContext.Value;
            syncContext.SetMailbox(this);
            SynchronizationContext.SetSynchronizationContext(syncContext);
            foreach (var action in actions)
            {
                action.Action(action.State);
            }
            SynchronizationContext.SetSynchronizationContext(oldSyncContext);

            QueueNextWork();
        }

        void QueueNextWork()
        {
            ActionCallback action = new ActionCallback();
            List<ActionCallback>? actions = null;
            lock (_actions)
            {
                if (_actions.Count==0)
                {
                    _inProgress = false;
                    return;
                }

//                if (_actions.Count > 1)
//                {
//                    actions = new List<ActionCallback>();
//                    for (int i = 0; i < 100 && _actions.Count>0; ++i)
//                    {
//                        actions.Add(_actions.Dequeue());
//                    }
//                }
//                else
                {
                    action = _actions.Dequeue();
                }
            }

            if (actions==null)
            {
                ThreadPool.QueueUserWorkItem(RunAction, action, true);
            }
            else
            {
                ThreadPool.QueueUserWorkItem(RunBatch, actions, preferLocal: false);
            }
        }

        class MailboxSynchronizationContext : SynchronizationContext
        {
            OldSimpleMailbox _mailbox;

            public MailboxSynchronizationContext(OldSimpleMailbox mailbox)
            {
                _mailbox = mailbox;
            }

            public void SetMailbox(OldSimpleMailbox mailbox) => _mailbox = mailbox;

            public override void Post(SendOrPostCallback d, object? state)
            {
                _mailbox.Execute(new ActionCallback(d,state));
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotImplementedException();
            }
        }
    }
}