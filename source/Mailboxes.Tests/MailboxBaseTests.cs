// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Mailboxes.Tests
{
    public abstract class MailboxBaseTests : Mailbox.IEventHandler
    {
        readonly ITestOutputHelper _output;

        protected Action<Exception>? OnException { get; set; }

        protected MailboxBaseTests(ITestOutputHelper output)
        {
            _output = output;
        }

        protected abstract Mailbox CreateMailbox();

        [Fact]
        public async Task MailboxIsNotReentrant()
        {
            int check = 0;
            var sut = CreateMailbox();

            using var mre1 = new ManualResetEventSlim();
            var tcs1 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tcs2 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = Task.Run(() => Test(tcs1));
            _ = Task.Run(() => Test(tcs2));
            mre1.Set();

            void Test(TaskCompletionSource<bool> tcs)
            {
                sut.Execute(() =>
                {
                    var result = Interlocked.Increment(ref check);
                    Assert.Equal(1, result);
                    mre1.Wait();
                    Interlocked.Decrement(ref check);
                    tcs.SetResult(true);
                });
            }

            await Task.WhenAll(tcs1.Task, tcs2.Task);
        }

        [Fact]
        public async Task MailboxIsNotReentrantUnderStress()
        {
            int check = 0;
            var sut = CreateMailbox();

            const int concurrency = 10000;
            var tasks = new Task[concurrency];

            Parallel.For(0, concurrency, i =>
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                tasks[i] = tcs.Task;

                Test(tcs);
            });

            void Test(TaskCompletionSource<bool> tcs)
            {
                sut.Execute(() =>
                {
                    var result = Interlocked.Increment(ref check);
                    Assert.Equal(1, result);
                    Thread.SpinWait(5);
                    Interlocked.Decrement(ref check);
                    tcs.SetResult(true);
                });
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task CancellationWaitsForMessage()
        {
            var sut = CreateMailbox();

            using var cts = new CancellationTokenSource();
            using var mre1 = new ManualResetEventSlim();
            using var mre2 = new ManualResetEventSlim();

            var test = Test(cts.Token);
            mre2.Wait();
            cts.Cancel();
            Assert.True(cts.Token.IsCancellationRequested);
            mre1.Set();
            await test;

            async Task Test(CancellationToken ct)
            {
                await sut.Include(ref ct);
                mre2.Set();
                mre1.Wait();
                Assert.False(ct.IsCancellationRequested);
                await Task.Run(() =>
                {
                    while (!ct.IsCancellationRequested)
                        Thread.Sleep(1);
                });
                Assert.True(ct.IsCancellationRequested);
            }
        }

        [Fact]
        public async Task MailboxStopsProcessingMessagesOnStop()
        {
            var sut = CreateMailbox();

            using var mre1 = new ManualResetEventSlim();
            using var mre2 = new ManualResetEventSlim();
            bool actionExecuted = false;

            sut.Execute(() =>
            {
                mre1.Set();
                mre2.Wait();
            });
            mre1.Wait();
            sut.Execute(() => { actionExecuted = true; });
            var stopTask = sut.StopAsync();
            mre2.Set();
            await stopTask;
            Assert.False(actionExecuted);
        }

        [Fact]
        public async Task StoppedMailboxDoesNotStart()
        {
            var sut = CreateMailbox();
            var stopTask = sut.StopAsync();

            bool actionExecuted = false;
            sut.Execute(() => { actionExecuted = true; });
            await stopTask;
            Assert.False(actionExecuted);
        }

        [Fact]
        public async Task StopWorksUnderStress()
        {
            const int msgCount = 1000;
            int minCounter = int.MaxValue;
            int maxCounter = 0;

            for (int r = 0; r < 100; ++r)
            {
                var sut = CreateMailbox();

                using var mre1 = new ManualResetEventSlim();
                int counter = 0;
                bool stopSending = false;

                var sendTask = Task.Run(() =>
                {
                    for (int i = 0; i < msgCount && !stopSending; ++i)
                    {
                        sut.Execute(Test);
                    }
                });

                mre1.Wait();
                await sut.StopAsync();

                // Confirm we eventual stop
                var tempCounter = counter;
                Assert.True(tempCounter < msgCount);
                minCounter = Math.Min(minCounter, tempCounter);
                maxCounter = Math.Max(maxCounter, tempCounter);

                // Confirm this is stable
                Thread.Sleep(1);
                Assert.True(counter == tempCounter);

                // Stop sending and let this wrap up
                stopSending = true;
                await sendTask;

                void Test()
                {
                    sut.Execute(() =>
                    {
                        Interlocked.Increment(ref counter);
                        if (counter == 3)
                        {
                            mre1.Set();
                        }

                        Thread.Yield();
                    });
                }
            }

            _output.WriteLine($"Range = [{minCounter},{maxCounter}]");
        }

        [Fact]
        public void AsyncLocalFlowsCorrectlyUsingAwait()
        {
            var sut = CreateMailbox();

            using var mre1 = new ManualResetEventSlim();

            var target = new AsyncLocal<int>();
            target.Value = 1;

            Test();

            async void Test()
            {
                Assert.Equal(1, target.Value);
                await sut;
                Assert.Equal(1, target.Value);
                target.Value = 2;
                await InnerTest();
                Assert.Equal(2, target.Value);
            }

            async Task InnerTest()
            {
                Assert.Equal(2, target.Value);
                await sut;
                Assert.Equal(2, target.Value);
                mre1.Set();
            }

            mre1.Wait();
            Assert.Equal(1, target.Value);
        }

        [Fact]
        public void AsyncLocalFlowsCorrectlyUsingExecute()
        {
            var sut = CreateMailbox();

            using var mre1 = new ManualResetEventSlim();

            var target = new AsyncLocal<int>();
            target.Value = 1;

            sut.Execute(Test);

            void Test()
            {
                Assert.Equal(1, target.Value);
                target.Value = 2;
                sut.Execute(InnerTest);
            }

            void InnerTest()
            {
                Assert.Equal(2, target.Value);
                mre1.Set();
            }

            mre1.Wait();
            Assert.Equal(1, target.Value);
        }

        [Fact]
        public void StopsAfterExceptionFromExecute()
        {
            var sut = CreateMailbox();
            sut.EventHandler = this;
            using var mre = new ManualResetEventSlim();
            OnException = ex => sut.StopAsync();

            sut.Execute(() => throw new System.Exception("Boom."));
            sut.Execute(() => mre.Set());

            Assert.False(mre.Wait(25));
        }

        /// <summary>
        /// This test is to demonstrate that calling an async void is OK, but awaiting a mailbox
        /// in an async void method is NOT OK.
        /// </summary>
        [Fact]
        public void StopsAfterExceptionFromInnerAsyncVoid()
        {
            var sut = CreateMailbox();
            sut.EventHandler = this;
            using var mre = new ManualResetEventSlim();
            OnException = ex => sut.StopAsync();

            async Task Test()
            {
                await sut;
                InnerTest();
            }

            async void InnerTest()
            {
                // Note that this exception will be queued up to be thrown as an individual action
                // https://github.com/dotnet/corefx/blob/3c30e11dbc7c381aa89648d06048766026eed96e/src/Common/src/CoreLib/System/Runtime/CompilerServices/AsyncVoidMethodBuilder.cs#L116
                throw new Exception("Boom");
            }

            Assert.ThrowsAsync<Exception>(Test);
        }

        [Fact]
        public void ContinuesAfterException()
        {
            var sut = CreateMailbox();
            sut.EventHandler = this;
            using var mre = new ManualResetEventSlim();

            sut.Execute(() => throw new Exception("Boom."));
            sut.Execute(() => mre.Set());

            Assert.True(mre.Wait(25));
        }

        [Fact]
        public void TaskFromExecuteAsyncCompletes()
        {
            var sut = CreateMailbox();
            sut.EventHandler = this;
            using var mre = new ManualResetEventSlim();
            var exception = default(Exception);
            OnException = ex => exception = ex;

            var task = sut.ExecuteAsync(async () => { await Task.Delay(0); });

            task.ContinueWith(_ => mre.Set());

            mre.Wait();
            Assert.Null(exception);
            Assert.True(task.IsCompletedSuccessfully);
        }

        [Fact]
        public void TaskFromExecuteAsyncFaults()
        {
            var sut = CreateMailbox();
            sut.EventHandler = this;
            using var mre = new ManualResetEventSlim();
            var exception = default(Exception);
            OnException = ex => exception = ex;

            var task = sut.ExecuteAsync(async () =>
            {
                await Task.Delay(0);
                throw new Exception("Boom.");
            });

            task.ContinueWith(_ => mre.Set());

            mre.Wait();
            Assert.Null(exception);
            Assert.True(task.IsFaulted);
            Assert.NotNull(task.Exception);
            Assert.IsType<Exception>(task.Exception?.InnerException);
        }

        [Fact]
        public void TaskFromExecuteAsyncCancels()
        {
            var sut = CreateMailbox();
            sut.EventHandler = this;
            using var mre = new ManualResetEventSlim();
            var exception = default(Exception);
            OnException = ex => exception = ex;

            var task = sut.ExecuteAsync(async () =>
            {
                await Task.Delay(0);
                throw new OperationCanceledException();
            });

            task.ContinueWith(_ => mre.Set());

            mre.Wait();
            Assert.Null(exception);
            Assert.True(task.IsCanceled);
        }

        [Fact]
        public void TaskFromExecuteAsyncOfTCompletes()
        {
            var sut = CreateMailbox();
            sut.EventHandler = this;
            using var mre = new ManualResetEventSlim();
            var exception = default(Exception);
            OnException = ex => exception = ex;

            var task = sut.ExecuteAsync(async () =>
            {
                await Task.Delay(0);
                return 1;
            });

            task.ContinueWith(_ => mre.Set());

            mre.Wait();
            Assert.Null(exception);
            Assert.True(task.IsCompletedSuccessfully);
            Assert.Equal(1, task.Result);
        }

        [Fact]
        public void TaskFromExecuteAsyncOfTFaults()
        {
            var sut = CreateMailbox();
            sut.EventHandler = this;
            using var mre = new ManualResetEventSlim();
            var exception = default(Exception);
            OnException = ex => exception = ex;

            var task = sut.ExecuteAsync(async () =>
            {
                await Task.Delay(0);
                throw new Exception("Boom.");
#pragma warning disable 162
                return 1;
#pragma warning restore 162
            });

            task.ContinueWith(_ => mre.Set());

            mre.Wait();
            Assert.Null(exception);
            Assert.True(task.IsFaulted);
            Assert.NotNull(task.Exception);
            Assert.IsType<Exception>(task.Exception?.InnerException);
        }

        [Fact]
        public void TaskFromExecuteAsyncOfTCancels()
        {
            var sut = CreateMailbox();
            sut.EventHandler = this;
            using var mre = new ManualResetEventSlim();
            var exception = default(Exception);
            OnException = ex => exception = ex;

            var task = sut.ExecuteAsync(async () =>
            {
                await Task.Delay(0);
                throw new OperationCanceledException();
#pragma warning disable 162
                return 1;
#pragma warning restore 162
            });

            task.ContinueWith(_ => mre.Set());

            mre.Wait();
            Assert.Null(exception);
            Assert.True(task.IsCanceled);
        }

        void Mailbox.IEventHandler.OnException(Exception ex)
        {
            OnException?.Invoke(ex);
        }
    }
}