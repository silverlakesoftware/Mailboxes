// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

namespace Mailboxes.Benchmarks.Skynet.Mailboxes
{
    class SkynetActor
    {
        readonly Action<long> _resultCallback;
        readonly Mailbox _mailbox = new SimpleMailbox();

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
                if (level == 1)
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

                if (_todo == 0)
                {
                    _resultCallback(_count);
                }
            });
        }
    }

    class RootActor
    {
        readonly Mailbox _mailbox = new SimpleMailbox();

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