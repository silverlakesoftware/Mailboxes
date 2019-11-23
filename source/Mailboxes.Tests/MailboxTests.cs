// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Mailboxes.Tests
{
    public class MailboxTests
    {
        [Fact]
        public async Task ContextIsPassedToMailbox()
        {
            var mailbox = new ContextTestMailbox();

            await mailbox;
            await mailbox.WithContext("a");
            await mailbox;

            Assert.Equal(new[] {"1", "2a", "3"}, mailbox.Contexts);
        }

        [Fact]
        public async Task TaskContinuationContextIsPassedToMailbox()
        {
            var mailbox = new ContextTestMailbox();

            await mailbox;
            await Task.Delay(1);
            await Task.Delay(1).ContinueWithContext("a");
            await Task.Delay(1);

            Assert.Equal(new[] {"1", "2", "3a", "4"}, mailbox.Contexts);
        }

        [Fact]
        public async Task TaskContinuationContextIsPassedToMailboxWithThread()
        {
            var mailbox = new ContextTestMailbox();

            await mailbox;
            await Task.Run(() => { });
            await Task.Run(() => { }).ContinueWithContext("a");
            await Task.Run(() => { });

            Assert.Equal(new[] {"1", "2", "3a", "4"}, mailbox.Contexts);
        }

        [Fact]
        public async Task TaskContinuationContextIsPassedToMailboxWithNestedContext()
        {
            var mailbox = new ContextTestMailbox();

            await mailbox;
            await Task.Run(() => { });
            await Task.Run(async () =>
            {
                await mailbox;
                await Task.Run(() => { return true; }).ContinueWithContext("b");
                return true;
            }).ContinueWithContext("a");
            await Task.Run(() => { });

            Assert.Equal(new[] {"1", "2", "3", "4b", "5a", "6"}, mailbox.Contexts);
        }

        class ContextTestMailbox : SimpleMailbox
        {
            readonly List<string> _contexts = new List<string>();
            int _nextActionIndex;

            protected override void DoQueueAction(in MailboxAction action, object? actionContext)
            {
                lock (_contexts)
                {
                    var actionIndex = ++_nextActionIndex;
                    _contexts.Add(actionIndex.ToString() + (actionContext as string ?? ""));
                }

                base.DoQueueAction(action, actionContext);
            }

            public List<string> Contexts => _contexts;
        }
    }
}