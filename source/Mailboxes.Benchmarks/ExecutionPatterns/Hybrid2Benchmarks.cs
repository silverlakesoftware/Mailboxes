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
    public class Hybrid2Benchmarks
    {
        readonly Queue<ActionCandidate> _actions = new Queue<ActionCandidate>();
        readonly SampleCallTarget _target = new SampleCallTarget();
        readonly object _message = new object();
        readonly SendOrPostCallback _targetAction;
        readonly ActionClassCandidate.SpecialActionCandidate2 _sacTarget;

        public Hybrid2Benchmarks()
        {
            var target = _target;
            _targetAction = state => target.DoSomething(state!);
            _sacTarget = new ActionClassCandidate.SpecialActionCandidate2(_target);
        }

        [Benchmark]
        public void ActionCall()
        {
            Execute(() => { });
        }

        [Benchmark]
        public void SendOrPostCallbackCall()
        {
            Execute(o => ((Hybrid2Benchmarks)o!).DoIt(), this);
        }

        void DoIt() { }

        [Benchmark]
        public void MessageMethodCall()
        {
            Execute(_target, _message, (t, m) => t.DoSomething(m));
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


        [Benchmark]
        public void MessageMethodCallUsingSendOrPost()
        {
            Execute(o =>
            {
                var args = (ArgBag<SampleCallTarget, object>)o!;
                args.Arg1.DoSomething(args.Arg2);
            }, new ArgBag<SampleCallTarget, object>(_target, _message));
        }

        [Benchmark]
        public void MessageMethodCallUsingTuple()
        {
            Execute((_target, _message), args => args._target.DoSomething(args._message));
        }

        [Benchmark]
        public void MessageMethodCallCustomActionPrealloced()
        {
            Execute(new ActionCandidate(_sacTarget, _message));
        }

        [Benchmark]
        public void MessageMethodCallCustomAction()
        {
            Execute(new ActionCandidate(new ActionClassCandidate.SpecialActionCandidate(_message), _target));
        }

        [Benchmark]
        public void MessageMethodCallSwitchConditional()
        {
            Execute(() => { });
            Execute(new ActionCandidate(_sacTarget, _message));
        }

        [Benchmark]
        public void MessageMethodCallNotPrealloced()
        {
            var target = _target;
            SendOrPostCallback targetAction = state => target.DoSomething(state!);
            Execute(new ActionCandidate(targetAction, _message));
        }

        [Benchmark]
        public void MesageMethodCallPrealloced()
        {
            Execute(new ActionCandidate(_targetAction, _message));
        }

        [Benchmark]
        public void ActionCallWithCapture()
        {
            int a = 0;
            Execute(a, v => ++v);
        }

        // [Benchmark]
        // public bool IsNull()
        // {
        //     var actionCandidate = new ActionCandidate();
        //     return actionCandidate.IsNull();
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute(Action action)
        {
            Execute(CallAction, action);
            //Execute(new ActionCandidate(ActionClassCandidate.ActionInstance, action));
        }

        static readonly SendOrPostCallback CallAction = delegate(object? state) { ((Action)state!).Invoke(); };

        class RequireStruct<T> where T : struct { }
        class RequireClass<T> where T : class { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute<T>(in T arg1, Action<T> action) where T : struct
        {
            Execute(new ActionCandidate(ActionClassCandidate.From(in arg1), action));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute<T1, T2>(T1 arg1, T2 arg2, Action<T1, T2> action)
        {
            Execute(new ActionCandidate(ActionClassCandidate.From(arg1, arg2), action));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute(SendOrPostCallback callback, object? state)
        {
            Execute(new ActionCandidate(callback, state!));
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

        readonly struct ActionCandidate
        {
            readonly object? _action;
            readonly object? _state;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ActionCandidate(SendOrPostCallback action, object state)
            {
                _action = action;
                _state = state;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ActionCandidate(ActionClassCandidate action, object state)
            {
                _action = action;
                _state = state;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsNull() => _action == null;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute()
            {
                if (_action is SendOrPostCallback callback)
                {
                    callback.Invoke(_state);
                }
                else
                {
                    ((ActionClassCandidate)_action!).Execute(_state!);
                }
            }
        }

        public abstract class ActionClassCandidate
        {
            public abstract void Execute(object state);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ActionClassCandidate From<T>(T state) where T : class => new ActionActionCandidate<T>(state);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ActionClassCandidate From<T>(in T state) where T : struct => new ActionActionCandidate<T>(in state);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static ActionClassCandidate From<T1, T2>(T1 state, T2 state2) => new ActionActionCandidate<T1, T2>(state, state2);
            
            class ActionActionCandidate<T> : ActionClassCandidate
            {
                T _state;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public ActionActionCandidate(in T state)
                {
                    _state = state;
                }

                public override void Execute(object state) => ((Action<T>)state!).Invoke(_state);
            }

            class ActionActionCandidate<T1, T2> : ActionClassCandidate
            {
                T1 _state;
                T2 _state2;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public ActionActionCandidate(in T1 state, in T2 state2)
                {
                    _state = state;
                    _state2 = state2;
                }

                public override void Execute(object state) => ((Action<T1, T2>)state!).Invoke(_state, _state2);
            }

            public class SpecialActionCandidate : ActionClassCandidate
            {
                object _state;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public SpecialActionCandidate(object state)
                {
                    _state = state;
                }

                public override void Execute(object state) => ((SampleCallTarget)state!).DoSomething(_state!);
            }

            public class SpecialActionCandidate2 : ActionClassCandidate
            {
                SampleCallTarget _state;

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public SpecialActionCandidate2(SampleCallTarget state)
                {
                    _state = state;
                }

                public override void Execute(object state) => _state.DoSomething(state!);
            }
        }
    }
}