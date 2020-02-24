// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;

namespace Mailboxes.Benchmarks.PingPong
{
    public interface IMessageReceiver
    {
        void Message(IMessageReceiver sender);
    }

    public class MailboxPingActor : IMessageReceiver
    {
        readonly Mailbox _mailbox = new ConcurrentMailbox();
        readonly SendOrPostCallback _messageAction;

        private readonly TaskCompletionSource<bool> _wgStop;
        private int _messageCount;
        private readonly int _batchSize;
        private int _batch;

        public MailboxPingActor(TaskCompletionSource<bool> wgStop, int messageCount, int batchSize)
        {
            _wgStop = wgStop;
            _messageCount = messageCount;
            _batchSize = batchSize;
            _messageAction = state => this.DoMessage((IMessageReceiver)state!);
        }

        public void Start(IMessageReceiver sender)
        {
            _mailbox.Execute(DoExecute);

            void DoExecute()
            {
                SendBatch(sender);
            }
        }

        public void Message(IMessageReceiver sender)
        {
            _mailbox.Execute(_messageAction, sender);
        }

        void DoMessage(IMessageReceiver sender)
        {
            _batch--;
            if (_batch > 0)
            {
                return;
            }
            
            if (!SendBatch(sender))
            {
                _wgStop.SetResult(true);
            }
        }

        bool SendBatch(IMessageReceiver sender)
        {
            if (_messageCount == 0)
            {
                return false;
            }

            for (var i = 0; i < _batchSize; i++)
            {
                sender.Message(this);
            }

            _messageCount -= _batchSize;
            _batch = _batchSize;
            return true;
        }
    }

    public class MalboxEchoActor : IMessageReceiver
    {
        readonly Mailbox _mailbox = new ConcurrentMailbox();
        readonly SendOrPostCallback _messageAction;

        public MalboxEchoActor()
        {
            _messageAction = state => DoMessage((IMessageReceiver)state!);
        }

        public void Message(IMessageReceiver sender)
        {
            _mailbox.Execute(_messageAction, sender);
        }

        void DoMessage(IMessageReceiver sender)
        {
            sender.Message(sender);
        }
    }
}