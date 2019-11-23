// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

// Created by Jamie da Silva on 10/13/2019 3:02 PM

using Xunit.Abstractions;

namespace Mailboxes.Tests
{
    public class SimpleMailboxTests : MailboxBaseTests
    {
        public SimpleMailboxTests(ITestOutputHelper output) : base(output) { }

        protected override Mailbox CreateMailbox() => new SimpleMailbox();
    }
}