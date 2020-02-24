// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;

namespace Mailboxes.Benchmarks.Skynet.Mailboxes
{
    class SkynetActorAwait
    {
        readonly Mailbox _mailbox = new SimpleMailbox();

        public async Task<long> Start(int level, long num)
        {
            await _mailbox;

            if (level == 1)
            {
                return num;
            }

            var startNum = num * 10;
            var tasks = new Task<long>[10];
            for (int i = 0; i < 10; ++i)
            {
                var child = new SkynetActorAwait();
                tasks[i] = child.Start(level - 1, startNum + i);
            }

            var results = await Task.WhenAll(tasks);

            long count = 0;
            for (int i = 0; i < 10; ++i)
            {
                count += results[i];
            }

            return count;
        }
    }

    class RootActorAwait
    {
        readonly Mailbox _mailbox = new SimpleMailbox();

        public async Task<long> Run()
        {
            await _mailbox;

            var actor = new SkynetActorAwait();
            var result = await actor.Start(5, 0);
            //Console.WriteLine(result);
            return result;
        }
    }
}