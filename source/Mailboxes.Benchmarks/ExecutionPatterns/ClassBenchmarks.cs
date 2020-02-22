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
    public class ClassBenchmarks
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
            Execute(o => ((ClassBenchmarks)o!).DoIt(), this);
        }

        void DoIt()  { }

        [Benchmark]
        public void MessageMethodCall()
        {
            Execute(_target, _message, (t, m) => t.DoSomething(m));
        }

        [Benchmark]
        public void MessageMethodCallUsingTuple()
        {
            Execute((_target, _message), args => args._target.DoSomething(args._message));
        }

        [Benchmark]
        public void ActionCallWithCapture()
        {
            int a = 0;
            Execute(a, v => ++v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute(Action action)
        {
            Execute(ActionCandidate.From(action));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute<T>(in T arg1, Action<T> action)
        {
            Execute(ActionCandidate.From(action, arg1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute<T1, T2>(in T1 arg1, in T2 arg2, Action<T1, T2> action)
        {
            Execute(ActionCandidate.From(action, arg1, arg2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute(SendOrPostCallback callback, object? state)
        {
            Execute(ActionCandidate.From(callback, state));
        }

        void Execute(in ActionCandidate actionCandidate)
        {
            _actions.Enqueue(actionCandidate);
            Execute();
        }

        void Execute()
        {
            var actionCandidate = _actions.Dequeue();
            actionCandidate.Execute();
        }

        public abstract class ActionCandidate
        {
            public static readonly ActionCandidate Null = new NullActionCandidate();

            public abstract void Execute();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ActionCandidate From(in SendOrPostCallback callback, in object? state) => new SendOrPostCallbackActionCandidate(callback, state);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ActionCandidate From(in Action action) => new ActionActionCandidate(action);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ActionCandidate From<T>(in Action<T> action, in T state) => new ActionActionCandidate<T>(action, state);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ActionCandidate From<T1,T2>(in Action<T1, T2> action, in T1 state, in T2 state2) => new ActionActionCandidate<T1,T2>(action, state, state2);

            class NullActionCandidate : ActionCandidate
            {
                internal NullActionCandidate() { }

                public override void Execute() { }
            }

            class ActionActionCandidate : ActionCandidate
            {
                Action _action;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public ActionActionCandidate(Action action)
                {
                    _action = action;
                }

                public override void Execute() => _action();
            }

            class ActionActionCandidate<T> : ActionCandidate
            {
                Action<T> _action;
                T _state;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public ActionActionCandidate(Action<T> action, in T state)
                {
                    _action = action;
                    _state = state;
                }

                public override void Execute() => _action(_state);
            }

            class ActionActionCandidate<T1, T2> : ActionCandidate
            {
                Action<T1, T2> _action;
                T1 _state;
                T2 _state2;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public ActionActionCandidate(Action<T1, T2> action, in T1 state, in T2 state2)
                {
                    _action = action;
                    _state = state;
                    _state2 = state2;
                }

                public override void Execute() => _action(_state, _state2);
            }

            class SendOrPostCallbackActionCandidate : ActionCandidate
            {
                SendOrPostCallback _callback;
                object? _state;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public SendOrPostCallbackActionCandidate(SendOrPostCallback callback, object? state)
                {
                    _callback = callback;
                    _state = state;
                }

                public override void Execute() => _callback(_state);
            }
        }
    }
}