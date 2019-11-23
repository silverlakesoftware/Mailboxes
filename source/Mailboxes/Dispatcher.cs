// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

namespace Mailboxes
{
    public abstract class Dispatcher
    {
        public abstract void Execute(Mailbox mailbox);
    }
}