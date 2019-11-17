// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 11/16/2019 8:21 PM

namespace Mailboxes.Tests
{
    public class ThreadPoolDispatcherTests : DispatcherBaseTests
    {
        protected override Dispatcher CreateDispatcher()
        {
            return new ThreadPoolDispatcher();
        }
    }
}