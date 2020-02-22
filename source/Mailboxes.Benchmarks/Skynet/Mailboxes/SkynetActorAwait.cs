// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

namespace Mailboxes.Benchmarks.Skynet.Mailboxes
{
    class SkynetActorAwait
    {
        readonly Func<long, Task> _resultCallback;
        readonly Mailbox _mailbox = new SimpleMailbox();

        long _count;
        int _todo = 10;

        public SkynetActorAwait(Func<long,Task> resultCallback)
        {
            _resultCallback = resultCallback;
        }

        public async Task Start(int level, long num)
        {
            await _mailbox;

            if (level == 1)
            {
                _ = _resultCallback(num);
                return;
            }

            var startNum = num * 10;
            for (int i = 0; i < 10; ++i)
            {
                var child = new SkynetActorAwait(n => Value(n));
                _ = child.Start(level - 1, startNum + i);
            }
        }

        public async Task Value(long num)
        {
            await _mailbox;

            _todo -= 1;
            _count += num;

            if (_todo == 0)
            {
                _ = _resultCallback(_count);
            }
        }
    }

    class RootActorAwait
    {
        readonly Mailbox _mailbox = new SimpleMailbox();

        public async Task<long> Run()
        {
            await _mailbox;

            var tcs = new TaskCompletionSource<long>();
            var actor = new SkynetActorAwait(l =>
            {
                tcs.SetResult(l);
                return Task.CompletedTask;
            });
            _ = actor.Start(5, 0);
            return await tcs.Task;
        }
    }
}