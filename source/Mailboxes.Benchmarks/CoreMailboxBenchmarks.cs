// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace Mailboxes.Benchmarks
{
    [MemoryDiagnoser]
    public class CoreMailboxBenchmarks
    {
        static CoreMailboxBenchmarks()
        {
            SetDoneAction = _ => _done = true;
            IsDoneFunc = () => _done;
        }

        [ParamsSource(nameof(MailboxTypeParams))]
        public MailboxTypeParam MailboxType { get; set; }

        public static IEnumerable<MailboxTypeParam> MailboxTypeParams()
        {
            yield return MailboxTypeParam.From<SimpleMailbox>();
            yield return MailboxTypeParam.From<ConcurrentMailbox>();
            yield return MailboxTypeParam.From<PriorityMailbox>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Mailbox CreateMailbox() => MailboxType.CreateMailbox();

        [Benchmark]
        public Mailbox Create() => CreateMailbox();

        [Benchmark]
        public Task<int> CreateAndOneCall()
        {
            return Test(CreateMailbox());
        }

        static async Task<int> Test(Mailbox mailbox)
        {
            await mailbox;
            return 42;
        }

        static volatile bool _done;
        static readonly SendOrPostCallback SetDoneAction;
        static readonly Func<bool> IsDoneFunc;

        [Benchmark]
        public void CreateAndOneDirectCall()
        {
            var mailbox = CreateMailbox();
            _done = false;

            mailbox.Execute(SetDoneAction, null);

            while (!_done)
                Thread.SpinWait(1);
        }

        [Benchmark(OperationsPerInvoke = 1000)]
        public Task<int> DirectIncrement()
        {
            var tcs = new TaskCompletionSource<int>();
            var state = new IncrementState(CreateMailbox(), tcs);

            Parallel.For(0, 1000, _ => state.DoDirectIncrement());

            return tcs.Task;
        }

        [Benchmark(OperationsPerInvoke = 1000)]
        public Task<int> AwaitIncrement()
        {
            var tcs = new TaskCompletionSource<int>();
            var state = new IncrementState(CreateMailbox(), tcs);

            Parallel.For(0, 1000, i => _ = state.DoAwaitIncrement());

            return tcs.Task;
        }

        [Benchmark(OperationsPerInvoke = 1000)]
        public Task<int[]> PairDirectIncrement()
        {
            var tcs1 = new TaskCompletionSource<int>();
            var tcs2 = new TaskCompletionSource<int>();
            var state1 = new IncrementState(CreateMailbox(), tcs1, 500);
            var state2 = new IncrementState(CreateMailbox(), tcs2, 500);

            Parallel.For(0, 1000, i =>
            {
                if (i % 2 == 0)
                    state1.DoDirectIncrement();
                else
                    state2.DoDirectIncrement();
            });

            return Task.WhenAll(tcs1.Task, tcs2.Task);
        }

        [Benchmark(OperationsPerInvoke = 1000)]
        public Task<int[]> PairAwaitIncrement()
        {
            var tcs1 = new TaskCompletionSource<int>();
            var tcs2 = new TaskCompletionSource<int>();
            var state1 = new IncrementState(CreateMailbox(), tcs1, 500);
            var state2 = new IncrementState(CreateMailbox(), tcs2, 500);

            Parallel.For(0, 1000, i =>
            {
                if (i % 2 == 0)
                    _ = state1.DoAwaitIncrement();
                else
                    _ = state2.DoAwaitIncrement();
            });

            return Task.WhenAll(tcs1.Task, tcs2.Task);
        }

        public readonly struct MailboxTypeParam
        {
            readonly string _name;
            readonly Func<Mailbox> _factory;

            public MailboxTypeParam(string name, Func<Mailbox> factory)
            {
                _name = name;
                _factory = factory;
            }

            public static MailboxTypeParam From<T>() where T : Mailbox, new() => new MailboxTypeParam(typeof(T).Name, () => new T());

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Mailbox CreateMailbox() => _factory();

            public override string ToString() => _name;
        }

        class IncrementState
        {
            Mailbox _mailbox;
            TaskCompletionSource<int> _tcs;
            int _limit;
            int _value;
            SendOrPostCallback _incrementAction;

            public IncrementState(Mailbox mailbox, TaskCompletionSource<int> tcs, int limit = 1000)
            {
                _mailbox = mailbox;
                _tcs = tcs;
                _limit = limit;
                _incrementAction = o => ((IncrementState)o!).DoIncrement();
            }

            public void DoDirectIncrement()
            {
                _mailbox.Execute(_incrementAction, this);
            }

            void DoIncrement()
            {
                ++_value;
                if (_value == _limit)
                {
                    _tcs.SetResult(_value);
                }
            }

            public async Task DoAwaitIncrement()
            {
                await _mailbox;
                ++_value;
                if (_value == _limit)
                {
                    _tcs.SetResult(_value);
                }
            }
        }
    }
}