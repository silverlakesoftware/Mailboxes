// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System.Threading;

namespace Mailboxes
{
    public readonly struct MailboxAction
    {
        public MailboxAction(SendOrPostCallback action, object? state)
        {
            Action = action;
            State = state;
        }

        public SendOrPostCallback Action { get; }

        public object? State { get; }
    }
}