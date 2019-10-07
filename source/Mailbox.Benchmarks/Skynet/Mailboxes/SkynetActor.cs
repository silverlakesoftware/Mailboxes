// Copyright © 2019, Silverlake Software LLC.  All Rights Reserved.
// SILVERLAKE SOFTWARE LLC CONFIDENTIAL INFORMATION

// Created by Jamie da Silva on 9/28/2019 9:51 PM

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mailboxes.Benchmarks.Skynet.Mailboxes
{
    class SkynetActor
    {
        readonly Action<long> _resultCallback;
        readonly OldMailbox _mailbox = new OldSimpleMailbox();

        long _count;
        int _todo = 10;

        public SkynetActor(Action<long> resultCallback)
        {
            _resultCallback = resultCallback;
        }

        public void Start(int level, long num)
        {
            //await _mailbox;
            _mailbox.Execute(() =>
            {
                if (level==1)
                {
                    _resultCallback(num);
                    return;
                }

                var startNum = num * 10;
                for (int i = 0; i < 10; ++i)
                {
                    var child = new SkynetActor(Value);
                    child.Start(level - 1, startNum + i);
                }
            });
        }

        public void Value(long num)
        {
            //await _mailbox;
            _mailbox.Execute(() =>
            {
                _todo -= 1;
                _count += num;

                if (_todo==0)
                {
                    _resultCallback(_count);
                }
            });
        }

//        public async Task<long> Execute(long size, long div)
//        {
//            await _mailbox;
//            if (size == 1)
//                return _ordinal;
//            var tasks = new List<Task<long>>((int)div);
//            for (long i = 0; i < div; ++i)
//            {
//                var childOrdinal = _ordinal + i * (size / div);
//                var actor = new SkynetActor(childOrdinal);
//                tasks.Add(actor.Execute(size / div, div));
//            }
//
//            await Task.WhenAll(tasks);
//            return tasks.Sum(t => t.Result);
//        }
    }

    class RootActor
    {
        readonly OldMailbox _mailbox = new OldSimpleMailbox();

        public async Task<long> Run()
        {
            await _mailbox;

            var tcs = new TaskCompletionSource<long>();
            var actor = new SkynetActor(l => tcs.SetResult(l));
            actor.Start(5, 0);
            return await tcs.Task;
        }
    }

}