// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

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