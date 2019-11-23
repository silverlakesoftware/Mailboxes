// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Mailboxes.Internal;
using Xunit;

namespace Mailboxes.Tests.Internal
{
    public class PairingHeapTests
    {
        [Fact]
        public void NewHeapIsEmpty()
        {
            var heap = new PairingHeap<int>();
            Assert.True(heap.IsEmpty);
            Assert.False(heap.TryFindMin(out var element));
        }

        [Fact]
        public void CanAddAndPeekSingleItem()
        {
            var heap = new PairingHeap<int>();
            heap.Add(1);
            Assert.True(heap.TryFindMin(out var result));
            Assert.False(heap.IsEmpty);
            Assert.Equal(1, result);
        }

        [Fact]
        public void CanAddAndRemoveSingleItem()
        {
            var heap = new PairingHeap<int>();
            heap.Add(1);
            heap.TryRemoveMin();
            Assert.True(heap.IsEmpty);
        }

        [Fact]
        public void FindsCorrectMinWithTwoItems()
        {
            var heap = new PairingHeap<int>();
            heap.Add(2);
            heap.Add(1);
            Assert.True(heap.TryFindMin(out var result));
            Assert.Equal(1, result);

            heap = new PairingHeap<int>();
            heap.Add(1);
            heap.Add(2);
            Assert.True(heap.TryFindMin(out result));
            Assert.Equal(1, result);
        }

        [Fact]
        public void TryRemoveIsSafeOnEmptyHeap()
        {
            var heap = new PairingHeap<int>();
            heap.TryRemoveMin();
            Assert.True(heap.IsEmpty);
        }

        [Fact]
        public void FindsCorrectMinWithManyItems()
        {
            var rng = new Random();
            var set = new List<int>();
            var heap = new PairingHeap<int>();
            for (int i = 0; i < 100; ++i)
            {
                var next = rng.Next(1, 1000);
                set.Add(next);
                heap.Add(next);
            }

            set.Sort();
            for (int i = 0; i < 100; ++i)
            {
                Assert.True(heap.TryFindMin(out var result));
                Assert.Equal(set[i], result);
                heap.TryRemoveMin();
            }
        }
    }
}