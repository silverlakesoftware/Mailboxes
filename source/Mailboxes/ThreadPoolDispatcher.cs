﻿// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;

namespace Mailboxes
{
    public class ThreadPoolDispatcher : Dispatcher
    {
        public static readonly ThreadPoolDispatcher Default = new ThreadPoolDispatcher();
        int _executionUnits = 100;

        public int ExecutionUnits
        {
            get => _executionUnits;
            set => _executionUnits = Math.Max(1, value - 1);
        }

        public override void Execute(Mailbox mailbox)
        {
#if NETSTANDARD
            ThreadPool.QueueUserWorkItem(m => ((ThreadPoolDispatcher)m.Dispatcher).WorkItemCallback(m), mailbox, true);
#else
            ThreadPool.QueueUserWorkItem(m => ((ThreadPoolDispatcher)((Mailbox)m).Dispatcher).WorkItemCallback((Mailbox)m), mailbox);
#endif

        }

        public void WorkItemCallback(Mailbox mailbox)
        {
            var action = mailbox.DequeueAction();
            if (action.IsNull())
            {
                mailbox.TryContinueRunning();
                return;
            }

            var oldSyncContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(mailbox.SyncContext);

            try
            {
                action.Execute();

                var executionUnits = _executionUnits - 1;
                for (int i = 0; i < executionUnits; ++i)
                {
                    action = mailbox.DequeueAction();
                    if (!action.IsNull())
                    {
                        action.Execute();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                mailbox.HandleException(ex);
            }

            SynchronizationContext.SetSynchronizationContext(oldSyncContext);
            mailbox.TryContinueRunning();
        }
    }
}