// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/21/2019 10:54 AM

using System;
using System.Collections.Generic;
using System.Threading;

namespace Mailboxes
{
    public class SimpleMailbox : Mailbox
    {
        bool _inProgress;
        readonly Queue<Action> _actions = new Queue<Action>();

        public override int QueueDepth => _actions.Count;

        protected override void Execute(Action action)
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

            ThreadPool.QueueUserWorkItem(RunAction, action);
        }

        void RunAction(object? actionObject)
        {
            if (!(actionObject is Action action))
                return;
            var oldSyncContext = SynchronizationContext.Current;
            var syncContext = new MailboxSynchronizationContext(this);
            SynchronizationContext.SetSynchronizationContext(syncContext);
            action();
            SynchronizationContext.SetSynchronizationContext(oldSyncContext);

            lock (_actions)
            {
                if (_actions.Count==0)
                {
                    _inProgress = false;
                    return;
                }

                action = _actions.Dequeue();
            }

            ThreadPool.QueueUserWorkItem(RunAction, action);
        }

        class MailboxSynchronizationContext : SynchronizationContext
        {
            readonly SimpleMailbox _mailbox;

            public MailboxSynchronizationContext(SimpleMailbox mailbox)
            {
                _mailbox = mailbox;
            }

            public override void Post(SendOrPostCallback d, object? state)
            {
                _mailbox.Execute(() => d(state));
            }

            public override void Send(SendOrPostCallback d, object? state)
            {
                throw new NotImplementedException();
            }
        }
    }
}