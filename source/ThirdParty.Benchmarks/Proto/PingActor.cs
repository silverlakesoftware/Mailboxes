// From: https://github.com/Blind-Striker/actor-model-benchmarks/blob/master/src/Akka.Net/Akka.Net.Skynet/Program.cs
// Apache 2 License

using System.Threading.Tasks;
using Proto;

namespace ThirdParty.Benchmarks.Proto
{
    public class Msg
    {
        public Msg(PID sender)
        {
            Sender = sender;
        }

        public PID Sender { get; }
    }

    public class Start
    {
        public Start(PID sender)
        {
            Sender = sender;
        }

        public PID Sender { get; }
    }

    public class EchoActor : IActor
    {
        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Msg msg:
                    context.Send(msg.Sender, msg);
                    break;
            }
            return Actor.Done;
        }
    }

    public class PingActor : IActor
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

        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case Start s:
                    SendBatch(context, s.Sender);
                    break;
                case Msg m:
                    _batch--;

                    if (_batch > 0)
                    {
                        break;
                    }

                    if (!SendBatch(context, m.Sender))
                    {
                        _wgStop.SetResult(true);
                    }
                    break;
            }
            return Actor.Done;
        }

        private bool SendBatch(IContext context, PID sender)
        {
            if (_messageCount == 0)
            {
                return false;
            }

            var m = new Msg(context.Self);

            for (var i = 0; i < _batchSize; i++)
            {
                context.Send(sender, m);
            }

            _messageCount -= _batchSize;
            _batch = _batchSize;
            return true;
        }
    }
}