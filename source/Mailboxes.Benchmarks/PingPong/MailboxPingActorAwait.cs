// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Tasks;

namespace Mailboxes.Benchmarks.PingPong
{
    public interface IMessageReceiverAwait
    {
        Task Message(IMessageReceiverAwait sender);
    }

    public class MailboxPingActorAwait : IMessageReceiverAwait
    {
        readonly Mailbox _mailbox = new ConcurrentMailbox();

        private readonly TaskCompletionSource<bool> _wgStop;
        private int _messageCount;
        private readonly int _batchSize;
        private int _batch;

        public MailboxPingActorAwait(TaskCompletionSource<bool> wgStop, int messageCount, int batchSize)
        {
            _wgStop = wgStop;
            _messageCount = messageCount;
            _batchSize = batchSize;
        }

        public async Task Start(IMessageReceiverAwait sender)
        {
            await _mailbox;
            SendBatch(sender);
        }

        public async Task Message(IMessageReceiverAwait sender)
        {
            await _mailbox;
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

        bool SendBatch(IMessageReceiverAwait sender)
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

    public class MalboxEchoActorAwait : IMessageReceiverAwait
    {
        readonly Mailbox _mailbox = new ConcurrentMailbox();

        public async Task Message(IMessageReceiverAwait sender)
        {
            await _mailbox;
            _ = sender.Message(sender);
        }
    }
}