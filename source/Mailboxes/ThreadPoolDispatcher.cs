// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 1:59 PM

using System;
using System.Threading;

namespace Mailboxes
{
    public class ThreadPoolDispatcher : Dispatcher
    {
        public static readonly Dispatcher Default = new ThreadPoolDispatcher();

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
            SynchronizationContext.SetSynchronizationContext(mailbox.SyncContext);

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
    }
}