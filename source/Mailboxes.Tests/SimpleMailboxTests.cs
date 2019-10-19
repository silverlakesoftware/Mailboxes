// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 10/13/2019 3:02 PM

using Xunit.Abstractions;

namespace Mailboxes.Tests
{
    public class SimpleMailboxTests : MailboxTests
    {
        public SimpleMailboxTests(ITestOutputHelper output) : base(output) { }

        protected override Mailbox CreateMailbox() => new SimpleMailbox();
    }
}