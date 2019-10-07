// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 3:41 PM

using System.Collections.Concurrent;

namespace Mailboxes
{
    public class ConcurrentMailbox : Mailbox
    {
        readonly ConcurrentQueue<ActionCallback> _actions = new ConcurrentQueue<ActionCallback>();

        internal override bool QueueAction(in ActionCallback action)
        {
            lock (_actions)
            {
                if (_actions.IsEmpty)
                    return true;
                _actions.Enqueue(action);
            }

            return false;
        }

        internal override ActionCallback DequeueAction()
        {
            lock (_actions)
            {
                return _actions.TryDequeue(out var result) ? result : new ActionCallback();
            }
        }
    }
}