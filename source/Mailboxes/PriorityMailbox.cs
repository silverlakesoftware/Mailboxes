// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Mailboxes.Internal;

namespace Mailboxes
{
    public class PriorityMailbox : Mailbox
    {
        readonly PairingHeap<QueueNode> _actions;

        public PriorityMailbox() : this(null, null) { }

        public PriorityMailbox(Dispatcher? dispatcher) : this(null, dispatcher) { }

        public PriorityMailbox(IComparer<object?>? contextComparer) : this(contextComparer, null) { }

        public PriorityMailbox(IComparer<object?>? contextComparer, Dispatcher? dispatcher)
            : base(dispatcher)
        {
            _actions = new PairingHeap<QueueNode>(new QueueNodeComparer(contextComparer ?? DefaultComparer.Instance));
        }

        protected override void DoQueueAction(in MailboxAction action, object? actionContext)
        {
            lock (_actions)
            {
                _actions.Add(new QueueNode(action, actionContext));
            }
        }

        internal override bool IsEmpty
        {
            get
            {
                lock (_actions)
                {
                    return _actions.IsEmpty;
                }
            }
        }

        protected override MailboxAction DoDequeueAction()
        {
            lock (_actions)
            {
                if (!_actions.TryFindMin(out var queueNode))
                {
                    return new MailboxAction();
                }

                _actions.TryRemoveMin();
                return queueNode.Action;
            }
        }

        protected internal override void OnStop()
        {
            lock (_actions)
            {
                _actions.Clear();
            }
        }

        readonly struct QueueNode
        {
            readonly MailboxAction _action;
            readonly object? _actionContext;

            public QueueNode(MailboxAction action, object? actionContext)
            {
                _action = action;
                _actionContext = actionContext;
            }

            public MailboxAction Action => _action;

            public object? ActionContext => _actionContext;
        }

        class DefaultComparer : IComparer<object?>
        {
            public static readonly IComparer<object?> Instance = new DefaultComparer();

            DefaultComparer() { }

            public int Compare(object? x, object? y) => 0;
        }

        class QueueNodeComparer : IComparer<QueueNode>
        {
            readonly IComparer<object?> _comparer;

            public QueueNodeComparer(IComparer<object?> comparer)
            {
                _comparer = comparer;
            }

            public int Compare(QueueNode x, QueueNode y)
            {
                return _comparer.Compare(x.ActionContext, y.ActionContext);
            }
        }
    }
}