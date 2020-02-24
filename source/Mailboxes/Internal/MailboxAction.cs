// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Mailboxes.Internal
{
    public readonly struct MailboxAction
    {
        static readonly SendOrPostCallback CallAction = delegate (object? state) { ((Action)state!).Invoke(); };

        readonly object? _action;
        readonly object? _state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MailboxAction(SendOrPostCallback action, object? state)
        {
            _action = action;
            _state = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MailboxAction(Action action)
        {
            _action = CallAction;
            _state = action;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MailboxAction(IMailboxActionTarget actionTarget, object? state)
        {
            _action = actionTarget;
            _state = state;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNull() => _action == null;


        public void Execute()
        {
            if (_action is SendOrPostCallback callback)
            {
                callback.Invoke(_state);
            }
            else
            {
                ((IMailboxActionTarget)_action!).Execute(_state!);
            }
        }
    }
}