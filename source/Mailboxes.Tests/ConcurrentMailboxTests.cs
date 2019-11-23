// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using Xunit.Abstractions;

namespace Mailboxes.Tests
{
    public class ConcurrentMailboxTests : MailboxBaseTests
    {
        public ConcurrentMailboxTests(ITestOutputHelper output) : base(output) { }

        protected override Mailbox CreateMailbox() => new ConcurrentMailbox();
    }
}