// Copyright © 2020, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Runtime.CompilerServices;

namespace Mailboxes.Internal
{
    public interface IMailboxActionTarget 
    {
        void Execute(object state);
    }

    public class MailboxActionTarget<T1> : IMailboxActionTarget
    {
        T1 _arg1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MailboxActionTarget(in T1 arg1)
        {
            _arg1 = arg1;
        }

        public virtual void Execute(object state) => ((Action<T1>)state!).Invoke(_arg1);
    }
}