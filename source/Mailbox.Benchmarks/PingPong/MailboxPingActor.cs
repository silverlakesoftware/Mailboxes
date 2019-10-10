// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 10:19 AM

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

        private readonly TaskCompletionSource<bool> _wgStop;
        private int _messageCount;
        private readonly int _batchSize;
        private int _batch;

        public MailboxPingActor(TaskCompletionSource<bool> wgStop, int messageCount, int batchSize)
        {
            _wgStop = wgStop;
            _messageCount = messageCount;
            _batchSize = batchSize;
        }

        public void Start(IMessageReceiver sender)
        {
//            await _mailbox;
//            SendBatch(sender);
            _mailbox.Execute(DoExecute);

            void DoExecute()
            {
                SendBatch(sender);
            }
        }

        public void Message(IMessageReceiver sender)
        {
//            await _mailbox;
//            _batch--;
//            if (_batch > 0)
//            {
//                return;
//            }
//
//            if (!SendBatch(sender))
//            {
//                _wgStop.SetResult(true);
//            }

            _mailbox.Execute(DoMessage);

            void DoMessage()
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
        }

        private bool SendBatch(IMessageReceiver sender)
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

        public void Message(IMessageReceiver sender)
        {
//            await _mailbox;
//            sender.Message(sender);

            _mailbox.Execute(DoExecute);

            void DoExecute()
            {
                sender.Message(sender);
            }
        }
    }
}