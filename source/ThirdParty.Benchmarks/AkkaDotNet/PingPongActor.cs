// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/29/2019 10:19 AM

using System.Threading.Tasks;
using Akka.Actor;

namespace ThirdParty.Benchmarks.AkkaDotNet
{
    public class PingActor : UntypedActor
    {
        private readonly int _batchSize;
        private readonly TaskCompletionSource<bool> _wgStop;
        private int _batch;
        private int _messageCount;

        public PingActor(TaskCompletionSource<bool> wgStop, int messageCount, int batchSize)
        {
            _wgStop = wgStop;
            _messageCount = messageCount;
            _batchSize = batchSize;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Start s:
                    SendBatch(s.Sender);
                    break;
                case Msg m:
                    _batch--;
                    if (_batch > 0)
                    {
                        break;
                    }

                    if (!SendBatch(m.Sender))
                    {
                        _wgStop.SetResult(true);
                    }
                    break;
            }
        }

        private bool SendBatch(IActorRef sender)
        {
            if (_messageCount == 0)
            {
                return false;
            }

            var m = new Msg(Context.Self);

            for (var i = 0; i < _batchSize; i++)
            {
                sender.Tell(m);
            }

            _messageCount -= _batchSize;
            _batch = _batchSize;
            return true;
        }
    }

    public class Msg
    {
        public Msg(IActorRef sender)
        {
            Sender = sender;
        }

        public IActorRef Sender { get; }
    }

    public class Start
    {
        public Start(IActorRef sender)
        {
            Sender = sender;
        }

        public IActorRef Sender { get; }
    }

    public class EchoActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            if (message is Msg msg)
            {
                msg.Sender.Tell(msg);
            }
        }
    }
}