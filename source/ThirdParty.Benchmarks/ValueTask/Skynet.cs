// From https://github.com/atemerev/skynet

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

// ReSharper disable IdentifierTypo
#pragma warning disable IDE1006 // Naming Styles

namespace ThirdParty.Benchmarks.ValueTask
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Skynet
    {
        internal static Task<long> skynetThreadpoolValueTaskAsync(long num, long size, long div)
        {
            if (size == 1)
            {
                return Task.FromResult(num);
            }
            else
            {
                var tasks = new List<Task<long>>((int)div);
                for (var i = 0; i < div; i++)
                {
                    var sub_num = num + i * (size / div);
                    var task = Task.Run(() => skynetValueTaskAsync(sub_num, size / div, div).AsTask());
                    tasks.Add(task);
                }
                return Task.WhenAll(tasks).ContinueWith(skynetAggregator);
            }
        }

        static long skynetAggregator(Task<long[]> children)
        {
            long sumAsync = 0;
            var results = children.Result;
            for (var i = 0; i < results.Length; i++)
            {
                sumAsync += results[i];
            }
            return sumAsync;
        }

        private static ValueTask<long> skynetValueTaskAsync(long num, long size, long div)
        {
            if (size == 1)
            {
                return new ValueTask<long>(num);
            }
            else
            {
                long subtotal = 0;
                List<Task<long>>? tasks = null;

                for (var i = 0; i < div; i++)
                {
                    var sub_num = num + i * (size / div);
                    var task = skynetValueTaskAsync(sub_num, size / div, div);
                    if (task.IsCompleted)
                    {
                        subtotal += task.Result;
                    }
                    else
                    {
                        if (tasks == null)
                        {
                            tasks = new List<Task<long>>((int)div);
                        }
                        tasks.Add(task.AsTask());
                    }
                }

                if (tasks == null)
                {
                    return new ValueTask<long>(subtotal);
                }
                else if (subtotal > 0)
                {
                    tasks.Add(Task.FromResult(subtotal));
                }
                return new ValueTask<long>((Task.WhenAll(tasks).ContinueWith(skynetAggregator)));
            }
        }
    }
}