// From: https://github.com/Blind-Striker/actor-model-benchmarks/blob/master/src/Akka.Net/Akka.Net.Skynet/Program.cs
// Apache 2 License

using System.Threading.Tasks;
using Proto;

namespace ThirdParty.Benchmarks.Proto
{
    public class SkynetActor : IActor
    {
        public static readonly Props Props = Props.FromProducer(ProduceActor);
        private long _count;

        private int _todo = 10;

        public Task ReceiveAsync(IContext context)
        {
            var msg = context.Message;

            switch (msg)
            {
                case Start startMessage:
                    if (startMessage.Level == 1)
                    {
                        context.Send(context.Parent, startMessage.Num);
                        context.Self.Stop();

                        return Actor.Done;
                    }
                    else
                    {
                        var startNum = startMessage.Num * 10;

                        for (var i = 0; i < 10; i++)
                        {
                            var pid = context.Spawn(Props);
                            var childStart = new Start(startMessage.Level - 1, startNum + i);

                            context.Send(pid, childStart);
                        }

                        return Actor.Done;
                    }

                case long l:
                    _todo -= 1;
                    _count += l;

                    if (_todo == 0)
                    {
                        context.Send(context.Parent, _count);
                        context.Self.Stop();
                    }

                    return Actor.Done;
            }

            return Actor.Done;
        }

        private static SkynetActor ProduceActor()
        {
            return new SkynetActor();
        }

        public class Start
        {
            public Start(int level, long num)
            {
                Level = level;
                Num = num;
            }

            public int Level { get; }

            public long Num { get; }
        }
    }

    public class RootActor : IActor
    {
        private PID? _originalSender;

        public Task ReceiveAsync(IContext context)
        {
            var msg = context.Message;

            switch (msg)
            {
                case Run _:
                    var pid = context.Spawn(SkynetActor.Props);
                    var childStart = new SkynetActor.Start(5, 0);

                    _originalSender = context.Sender;
                    context.Send(pid, childStart);

                    break;
                case long l:

                    context.Send(_originalSender, new Result(l));

                    break;
            }

            return Actor.Done;
        }

        public class Run { }

        public class Result
        {
            public Result(long value)
            {
                Value = value;
            }

            public long Value { get; }
        }
    }
}