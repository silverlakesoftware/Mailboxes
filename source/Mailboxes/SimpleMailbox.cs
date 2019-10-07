// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 2:10 PM

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Mailboxes
{
    public class SimpleMailbox : Mailbox
    {
        readonly Queue<ActionCallback> _actions = new Queue<ActionCallback>(0);
        readonly ActionCallback[] _buffer = new ActionCallback[10];

        internal override bool QueueAction(in ActionCallback action)
        {
            _actions.Enqueue(action);
            return true;
        }

        internal override bool IsEmpty => _actions.Count==0;

        internal override ActionCallback DequeueAction() => _actions.Dequeue();

        internal override List<ActionCallback> DequeueActions(int max)
        {
            var result = new List<ActionCallback>(max);
            for (int i = 0; i < max && _actions.Count > 0; ++i)
                result.Add(_actions.Dequeue());
            return result;
        }

        internal int DequeueActions(out ActionCallback[] actions)
        {
            int count = Math.Min(10, _actions.Count);
            actions = _buffer;
            int i = 0;
            for (; i < count; ++i)
                actions[i] = _actions.Dequeue();
            return i;
        }
    }
}