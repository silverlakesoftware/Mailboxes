// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 10/13/2019 3:02 PM

namespace Mailboxes.Tests
{
    public class SimpleMailboxTests : MailboxTests
    {
        protected override Mailbox CreateMailbox() => new SimpleMailbox();
    }
}