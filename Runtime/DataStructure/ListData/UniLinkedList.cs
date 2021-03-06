﻿namespace UniModules.UniGame.Core.Runtime.DataStructure
{
    using System;
    using System.Collections;
    using Rx;
    using UniCore.Runtime.ObjectPool.Runtime;

    /// <summary>
    /// Lightweight property broker.
    /// </summary>
    [Serializable]
    public class UniLinkedList<T> : IUniLinkedList<T>
    {
        public ListNode<T> root;
        public ListNode<T> last;

        public ListNode<T> Add(T value)
        {
            var node = ClassPool.Spawn<ListNode<T>>();
            
            // subscribe node, node as subscription.
            var next = node.SetValue(value);
            if (root == null)
            {
                root = last = next;
            }
            else
            {
                last.Next     = next;
                next.Previous = last;
                last = next;
            }
            
            return next;
        }

        public void Remove(ListNode<T> node)
        {
            if (node == root)
            {
                root = node.Next;
            }
            if (node == last)
            {
                last = node.Previous;
            }

            if (node.Previous != null)
            {
                node.Previous.Next = node.Next;
            }
            if (node.Next != null)
            {
                node.Next.Previous = node.Previous;
            }
        }

        public void Dispose()
        {
            Release();
        }

        public void Release()
        {
            var node    = root;
            root = last = null;
            while (node != null)
            {
                var next = node.Next;
                node.Dispose();
                node = next;
            }
        }

        public void Apply(Action<T> action)
        {
            if (action == null)
                return;
            
            var node = root;
            while (node != null)
            {
                var next = node.Next;
                action(node.Value);
                node = next;
            }
        }

    }
}
