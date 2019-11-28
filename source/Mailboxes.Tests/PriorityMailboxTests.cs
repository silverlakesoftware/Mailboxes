// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Mailboxes.Tests
{
    public class PriorityMailboxTests : MailboxBaseTests
    {
        public PriorityMailboxTests(ITestOutputHelper output) : base(output) { }

        protected override Mailbox CreateMailbox() => new PriorityMailbox();

        [Fact]
        public void PrioritizesActions()
        {
            var sut = new PriorityMailbox(new TestComparer());
            using var mre = new ManualResetEventSlim();
            using var mre2 = new ManualResetEventSlim();
            bool executedStepA = false;
            bool executedStepB = false;
            sut.Execute(() => { mre.Wait(); });
            sut.Execute(() =>
            {
                executedStepB = true;
                Assert.True(executedStepA);
                mre2.Set();
            }, "b");
            sut.Execute(() =>
            {
                executedStepA = true;
                Assert.False(executedStepB);
            }, "a");

            mre.Set();
            mre2.Wait();

            Assert.True(executedStepB);
        }

        [Fact]
        public void SettingStateViaAwaitWorks()
        {
            var sut = new TestPriorityMailbox(new TestComparer());
            using var mre = new ManualResetEventSlim();
            using var mre2 = new ManualResetEventSlim();
            using var mre3 = new ManualResetEventSlim();
            using var mre4 = new ManualResetEventSlim();
            bool executedStepA = false;
            bool executedStepB = false;

            var task1 = Task.Run(async () =>
            {
                await sut;
                mre.Set();
                mre2.Wait();
            });

            mre.Wait();

            sut.OnAfterQueue = () => mre3.Set();
            var task2 = Task.Run(async () =>
            {
                await sut.WithContext("b");
                executedStepB = true;
                Assert.True(executedStepA);
            });
            mre3.Wait();

            sut.OnAfterQueue = () => mre4.Set();
            var task3 = Task.Run(async () =>
            {
                await sut.WithContext("a");
                executedStepA = true;
                Assert.False(executedStepB);
            });
            mre4.Wait();

            mre2.Set();

            Task.WaitAll(task1, task2, task3);
            Assert.True(executedStepB);
        }

        [Fact]
        public async Task SettingStateForTaskContinuationWorks()
        {
            var sut = new TestPriorityMailbox(new TestComparer());
            using var mre = new ManualResetEventSlim();
            using var mre2 = new ManualResetEventSlim();
            using var mre3 = new ManualResetEventSlim();
            using var mre4 = new ManualResetEventSlim();
            bool executedStepA = false;
            bool executedStepB = false;

            var task = Task.Run(async () =>
            {
                await sut;
                sut.OnAfterQueue = () =>
                {
                    sut.OnAfterQueue = () => { };
                    sut.Execute(() =>
                    {
                        executedStepA = true;
                        Assert.False(executedStepB);
                    }, "a");
                    mre.Set();
                };
                await Task.Delay(1).ContinueWithContext("b");
                executedStepB = true;
                Assert.True(executedStepA);
            });

            mre.Wait();
            await task;
            Assert.True(executedStepB);
        }

        public class TestComparer : IComparer<object?>
        {
            public int Compare(object? x, object? y) => string.CompareOrdinal(x as string, y as string);
        }

        public class TestPriorityMailbox : PriorityMailbox
        {

            public Action? OnAfterQueue { get; set; }

            public TestPriorityMailbox(IComparer<object?> contextComparer) : base(contextComparer)
            {
            }

            protected override void DoQueueAction(in MailboxAction action, object? actionContext)
            {
                base.DoQueueAction(action, actionContext);
                OnAfterQueue?.Invoke();
            }
        }
    }
}