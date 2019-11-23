// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using Xunit;

namespace Mailboxes.Tests
{
    public abstract class DispatcherBaseTests : Mailbox.IEventHandler
    {
        protected abstract Dispatcher CreateDispatcher();

        protected Action<Exception>? OnException { get; set; }

        [Fact]
        public void ExceptionIsSentToMailbox()
        {
            var sut = CreateDispatcher();
            var mailbox = new SimpleMailbox(sut) {EventHandler = this};
            using var mre = new ManualResetEventSlim();

            Exception? exception = null;
            OnException = ex =>
            {
                exception = ex;
                mre.Set();
                mailbox.StopAsync();
            };

            mailbox.Execute(() => throw new System.Exception("Boom."));

            mre.Wait(250);
            Assert.NotNull(exception);
            Assert.Equal("Boom.", exception?.Message);
        }

        [Fact]
        public void MailboxStopsAfterException()
        {
            var sut = CreateDispatcher();
            var mailbox = new SimpleMailbox(sut) {EventHandler = this};
            using var mre = new ManualResetEventSlim();
            OnException = ex => mailbox.StopAsync();

            mailbox.Execute(() => throw new System.Exception("Boom."));
            mailbox.Execute(() => mre.Set());

            Assert.False(mre.Wait(25));
        }

        [Fact]
        public void MailboxContinuesAfterException()
        {
            var sut = CreateDispatcher();
            var mailbox = new SimpleMailbox(sut) {EventHandler = this};
            using var mre = new ManualResetEventSlim();

            mailbox.Execute(() => throw new System.Exception("Boom."));
            mailbox.Execute(() => mre.Set());

            Assert.True(mre.Wait(25));
        }

        void Mailbox.IEventHandler.OnException(Exception ex)
        {
            OnException?.Invoke(ex);
        }
    }
}