// Copyright © 2020, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace Mailboxes.Benchmarks.ExecutionPatterns
{
    [MemoryDiagnoser]
    public class HybridBenchmarks
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
            Execute((SendOrPostCallback)(o => ((HybridBenchmarks)o!).DoIt()), this);
        }

        void DoIt() { }

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

        void Execute(Action action)
        {
            Execute(new ActionCandidate(a => (a as Action)!.Invoke(), action));
        }

        void Execute<T>(T arg1, Action<T> action)
        {
            Execute(new ActionCandidate(o => (o as ActionClassCandidate)!.Execute(), ActionClassCandidate.From(action, arg1)));
        }

        void Execute<T1, T2>(T1 arg1, T2 arg2, Action<T1, T2> action)
        {
            Execute(new ActionCandidate(o => (o as ActionClassCandidate)!.Execute(), ActionClassCandidate.From(action, arg1, arg2)));
        }

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
            actionCandidate.Action(actionCandidate.State);
        }

        readonly struct ActionCandidate
        {
            public ActionCandidate(SendOrPostCallback action, object? state)
            {
                Action = action;
                State = state;
            }

            public SendOrPostCallback Action { get; }

            public object? State { get; }
        }

        public abstract class ActionClassCandidate
        {
            public static readonly ActionClassCandidate Null = new NullActionCandidate();

            public abstract void Execute();

            public static ActionClassCandidate From(Action action) => new ActionActionCandidate(action);

            public static ActionClassCandidate From<T>(Action<T> action, T state) => new ActionActionCandidate<T>(action, state);

            public static ActionClassCandidate From<T1, T2>(Action<T1, T2> action, T1 state, T2 state2) => new ActionActionCandidate<T1, T2>(action, state, state2);

            class NullActionCandidate : ActionClassCandidate
            {
                internal NullActionCandidate() { }

                public override void Execute() { }
            }

            class ActionActionCandidate : ActionClassCandidate
            {
                readonly Action _action;

                public ActionActionCandidate(Action action)
                {
                    _action = action;
                }

                public override void Execute() => _action();
            }

            class ActionActionCandidate<T> : ActionClassCandidate
            {
                readonly Action<T> _action;
                readonly T _state;

                public ActionActionCandidate(Action<T> action, T state)
                {
                    _action = action;
                    _state = state;
                }

                public override void Execute() => _action(_state);
            }

            class ActionActionCandidate<T1, T2> : ActionClassCandidate
            {
                readonly Action<T1, T2> _action;
                readonly T1 _state;
                readonly T2 _state2;

                public ActionActionCandidate(Action<T1, T2> action, T1 state, T2 state2)
                {
                    _action = action;
                    _state = state;
                    _state2 = state2;
                }

                public override void Execute() => _action(_state, _state2);
            }
        }
    }
}