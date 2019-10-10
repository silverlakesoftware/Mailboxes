//// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
//// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION
//
//// Created by Jamie da Silva on 9/28/2019 1:47 PM
//
//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Threading;
//using System.Threading.Channels;
//using System.Threading.Tasks;
//
//namespace Mailboxes
//{
//    public class ChannelDispatcher : Dispatcher
//    {
//        //readonly BlockingCollection<ActionCallback> _actions = new BlockingCollection<ActionCallback>();
//        readonly Channel<MailboxAction> _actions = Channel.CreateUnbounded<MailboxAction>(new UnboundedChannelOptions {SingleReader = true, SingleWriter = false});
//        readonly List<Thread> _threads = new List<Thread>();
//        readonly ThreadLocal<DispatcherSynchronizationContext> _syncContext;
//
//        public static Dispatcher Default { get; } = new ChannelDispatcher();
//
//        public ChannelDispatcher()
//        {
//            for (int i = 0; i < 1; ++i)
//            {
//                var thread = new Thread(DispatchThread);
//                thread.Start();
//                _threads.Add(thread);
//            }
//
//            _syncContext = new ThreadLocal<DispatcherSynchronizationContext>(() => new DispatcherSynchronizationContext(this));
//        }
//
//        protected internal override void Queue(Mailbox mailbox, SendOrPostCallback d, object state)
//        {
//            //ThreadPool.QueueUserWorkItem(QueueAction, new ActionCallback(mailbox, d, state), true);
//            _actions.Writer.TryWrite(new MailboxAction(mailbox, d, state));
//            //_actions.Writer.TryWrite(new ActionCallback(mailbox, d, state));
//            //_actions.Add(new ActionCallback(mailbox, d, state));
//        }
//
//        void QueueAction(MailboxAction action) => _actions.Writer.TryWrite(action);
//
//        public override void Execute(in MailboxAction action)
//        {
//            if (action.Action==null)
//            {
//                return;
//            }
//            ThreadPool.QueueUserWorkItem(ExecuteCallback, action, true);
//        }
//
//        protected void Execute(List<MailboxAction> actions)
//        {
//            ThreadPool.QueueUserWorkItem(ExecuteItemsCallback, actions, true);
//        }
//
//        async void DispatchThread()
//        {
//            await foreach (var item in _actions.Reader.ReadAllAsync())
//            {
//                if (item.Mailbox==null)
//                {
//                    continue;
//                }
//                if (item.Action==null)
//                {
//                    if (item.Mailbox.IsEmpty)
//                    {
//                        item.Mailbox.InProgress = false; 
//                        continue;
//                    }
//
//                    var nextItem = item.Mailbox.DequeueAction();
//                    Execute(nextItem);
//
////                    var items = new List<ActionCallback>();
////                    while (!item.Mailbox.IsEmpty && items.Count < 100)
////                    {
////                        items.Add(item);
////                        item.Mailbox.DequeueAction();
////                    }
////                    Execute(items);
//                }
//                else
//                {
//                    if (item.Mailbox.InProgress)
//                        item.Mailbox.QueueAction(item);
//                    else
//                    {
//                        item.Mailbox.InProgress = true;
//                        Execute(item);
//                    }
//                }
//            }
//        }
//
//
//        void ExecuteItemsCallback(List<MailboxAction> actions)
//        {
//            var oldSynContext = SynchronizationContext.Current;
//
//            var syncContext = _syncContext.Value;
//            syncContext.SetMailbox(actions[0].Mailbox);
//            SynchronizationContext.SetSynchronizationContext(syncContext);
//
//            foreach (var action in actions)
//            {
//                action.Action(action.State);
//            }
//
//            //            for (int i = 0; i < 99; ++i)
//            //            {
//            //                action = action.Mailbox.DequeueAction();
//            //                if (action.Action == null)
//            //                    break;
//            //                action.Action(action.State);
//            //            }
//
//            SynchronizationContext.SetSynchronizationContext(oldSynContext);
//
//            Queue(actions[0].Mailbox, null, null);
//        }
//
//        void ExecuteCallback(MailboxAction action)
//        {
//            var oldSynContext = SynchronizationContext.Current;
//
//            var syncContext = _syncContext.Value;
//            syncContext.SetMailbox(action.Mailbox);
//            SynchronizationContext.SetSynchronizationContext(syncContext);
//            action.Action(action.State);
//
////            for (int i = 0; i < 99; ++i)
////            {
////                action = action.Mailbox.DequeueAction();
////                if (action.Action == null)
////                    break;
////                action.Action(action.State);
////            }
//
//            SynchronizationContext.SetSynchronizationContext(oldSynContext);
//
//            Queue(action.Mailbox, null, null);
//        }
//
//        struct MailboxState
//        {
//            public bool InProgress { get; set; }
//            public Queue<DispatchAction> DispatchActions { get; set; }
//        }
//      
//
//        class DispatcherSynchronizationContext : SynchronizationContext
//        {
//            readonly ChannelDispatcher _dispatcher;
//            Mailbox? _mailbox;
//
//            public DispatcherSynchronizationContext(ChannelDispatcher dispatcher)
//            {
//                _dispatcher = dispatcher;
//            }
//
//            public void SetMailbox(Mailbox mailbox) => _mailbox = mailbox;
//
//            public override void Post(SendOrPostCallback d, object? state)
//            {
//                _dispatcher.Queue(_mailbox, d, state);
//            }
//
//            public override void Send(SendOrPostCallback d, object? state)
//            {
//                throw new NotImplementedException();
//            }
//        }
//
//        public struct DispatchAction
//        {
//            public SendOrPostCallback Callback { get; set; }
//            public object? State { get; set; }
//        }
//
//        void ReleaseUnmanagedResources()
//        {
//            //_actions.CompleteAdding();
//            _actions.Writer.TryComplete();
//        }
//
//        public void Dispose()
//        {
//            ReleaseUnmanagedResources();
//            GC.SuppressFinalize(this);
//        }
//
//        ~ChannelDispatcher()
//        {
//            ReleaseUnmanagedResources();
//        }
//    }
//}