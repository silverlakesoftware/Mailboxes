// From: https://github.com/Blind-Striker/actor-model-benchmarks/blob/master/src/Akka.Net/Akka.Net.Skynet/Program.cs
// Apache 2 License

using System;
using Akka.Actor;

namespace ThirdParty.Benchmarks.AkkaDotNet
{
    public class SkynetActor : UntypedActor
    {
        public static readonly Props Props = Props.Create(() => new SkynetActor());
        private long _count;

        private int _todo = 10;

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Start start:
                    if (start.Level == 1)
                    {
                        Context.Parent.Tell(start.Num);
                        Context.Stop(Self);
                    }
                    else
                    {
                        var startNum = start.Num * 10;

                        for (var i = 0; i < 10; i++)
                        {
                            var childSkynetActor = Context.ActorOf(Props);
                            var childStart = new Start(start.Level - 1, startNum + i);

                            childSkynetActor.Tell(childStart);
                        }
                    }

                    break;
                case long l:
                    _todo -= 1;
                    _count += l;

                    if (_todo == 0)
                    {
                        Context.Parent.Tell(_count);
                        Context.Stop(Self);
                    }

                    break;
            }
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

    public class RootActor : UntypedActor
    {
        private int _num;
        private DateTime _startDateTime;
        IActorRef _originalSender;

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Run run:
                    _startDateTime = DateTime.Now;
                    _num = run.Num - 1;

                    var skynetActor = Context.ActorOf(SkynetActor.Props);
                    var childStart = new SkynetActor.Start(5, 0);

                    skynetActor.Tell(childStart);

                    _originalSender = Sender;

                    break;
                case long l:
                    var now = DateTime.Now;
                    var timeSpan = now - _startDateTime;

                    _originalSender.Tell(l);
                    CoordinatedShutdown.Get(Context.System).Run().Wait(5000);
                    break;
            }
        }

        public class Run
        {
            public Run(int num)
            {
                Num = num;
            }

            public int Num { get; }
        }
    }
}