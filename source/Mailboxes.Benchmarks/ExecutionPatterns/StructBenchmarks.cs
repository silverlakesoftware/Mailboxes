// Copyright © 2020, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace Mailboxes.Benchmarks.ExecutionPatterns
{
    [MemoryDiagnoser]
    public class StructBenchmarks
    {
        readonly Queue<ActionCandidate> _actions = new Queue<ActionCandidate>();
        readonly SampleCallTarget _target = new SampleCallTarget();
        readonly object _message = new object();

        [Benchmark]
        public void ActionCall()
        {
            Execute(() => { });
        }

        [Benchmark]
        public void SendOrPostCallbackCall()
        {
            Execute(o => ((StructBenchmarks)o!).DoIt(), this);
        }

        void DoIt() { }

        [Benchmark]
        public void MessageMethodCall()
        {
            var target = _target;
            var message = _message;
            Execute(o => target.DoSomething(o!), message);
        }

        class ArgBag<T1>
        {
            readonly T1 _arg1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ArgBag(in T1 arg1)
            {
                _arg1 = arg1;
            }

            public ref readonly T1 Arg1 => ref _arg1;
        }

        class ArgBag<T1, T2>
        {
            readonly T1 _arg1;
            readonly T2 _arg2;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ArgBag(in T1 arg1, in T2 arg2)
            {
                _arg1 = arg1;
                _arg2 = arg2;
            }

            public ref readonly T1 Arg1 => ref _arg1;

            public ref readonly T2 Arg2 => ref _arg2;
        }


        //[Benchmark]
        public void MessageMethodCallFromTuple1()
        {
            Execute(o => ((ArgBag<SampleCallTarget, object>)o!).Arg1.DoSomething(((ArgBag<SampleCallTarget, object>)o).Arg2), new ArgBag<SampleCallTarget, object>(_target, _message));
        }

        [Benchmark]
        public void MessageMethodCallFromTuple2()
        {
            Execute(o =>
            {
                var args = (ArgBag<SampleCallTarget, object>)o!;
                args.Arg1.DoSomething(args.Arg2);
            }, new ArgBag<SampleCallTarget, object>(_target, _message));
        }

        //[Benchmark]
        public void MessageMethodCallFromTuple3()
        {
            Execute(MessageMethodCallDelegate, new ArgBag<SampleCallTarget, object>(_target, _message));
        }

        static readonly SendOrPostCallback MessageMethodCallDelegate = delegate(object? state)
        {
            var args = (ArgBag<SampleCallTarget, object>)state!;
            args.Arg1.DoSomething(args.Arg2);
        };


        [Benchmark]
        public void ActionCallWithCapture()
        {
            int a = 0;
            // Execute(a =>
            // {
            //     ++a;
            // });

            Execute(o =>
            {
                int v = ((ArgBag<int>)o!).Arg1;
                ++v;
            }, new ArgBag<int>(a));
        }

        // [Benchmark]
        public bool IsNull()
        {
             var actionCandidate = new ActionCandidate();
             return actionCandidate.Action==null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute(Action action)
        {
            Execute(CallAction, action);
        }
        static readonly SendOrPostCallback CallAction = delegate (object? state) { ((Action)state!).Invoke(); };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute(SendOrPostCallback callback, object? state)
        {
            Execute(new ActionCandidate(callback, state));
        }

        void Execute(in ActionCandidate actionCandidate)
        {
            _actions.Enqueue(actionCandidate);
            Execute();
        }

        void Execute()
        {
            var actionCandidate = _actions.Dequeue();
            actionCandidate.Action!.Invoke(actionCandidate.State);
        }

        readonly struct ActionCandidate
        {
            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ActionCandidate(SendOrPostCallback action, object? state)
            {
                Action = action;
                State = state;
            }

            public SendOrPostCallback? Action { get; }

            public object? State { get; }
        }
    }
}