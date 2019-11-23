// Copyright © 2019, Silverlake Software LLC and Contributors (see NOTICES file)
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace Mailboxes.Internal
{
    public class PairingHeap<T>
    {
        readonly IComparer<T> _comparer;
        Node? _root;

        public PairingHeap()
        {
            _comparer = Comparer<T>.Default;
        }

        public PairingHeap(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public bool IsEmpty => _root == null;

        public void Clear()
        {
            _root = null;
        }

        public void Add(T element)
        {
            var node = new Node(element);
            if (IsEmpty)
            {
                _root = node;
                return;
            }

            _root = _root!.Meld(_comparer, node);
        }

        public bool TryFindMin(out T element)
        {
            if (IsEmpty)
            {
                element = default!;
                return false;
            }

            return _root!.TryFindMin(out element);
        }

        public void TryRemoveMin()
        {
            if (IsEmpty)
            {
                return;
            }

            _root = _root!.TryRemoveMin(_comparer);
        }

        public class Node
        {
            readonly T _element;
            List<Node>? _subHeaps;

            public Node(T element)
            {
                _element = element;
            }

            List<Node> EnsuredSubHeaps => _subHeaps ??= new List<Node>();

            internal bool TryFindMin(out T element)
            {
                element = _element;
                return true;
            }

            internal Node? TryRemoveMin(IComparer<T> comparer)
            {
                return _subHeaps == null ? null : MergePairs(comparer, _subHeaps);
            }

            Node? MergePairs(IComparer<T> comparer, List<Node> list)
            {
                var odd = list.Count % 2;
                var result = odd == 0 ? null : list[^1];
                var start = list.Count - odd;

                while (start > 0)
                {
                    var pair = list[start - 2].Meld(comparer, list[start - 1]);
                    result = result?.Meld(comparer, pair) ?? pair;
                    start -= 2;
                }

                return result;
            }

            internal Node Meld(IComparer<T> comparer, Node node)
            {
                // Check if it's empty
                if (node == null)
                {
                    return this;
                }

                if (comparer.Compare(_element, node._element) <= 0)
                {
                    EnsuredSubHeaps.Insert(0, node);
                    return this;
                }
                else
                {
                    node.EnsuredSubHeaps.Insert(0, this);
                    return node;
                }
            }
        }
    }
}