using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace HW3
{
    public interface IStack<T>
    {
        void Push(T item);
        bool TryPop(out T item);
        int Count { get; }
    }

    public class LockFreeStack<T> : IStack<T>
    {
        private Node<T> root;
        public int Count
        {
            get 
            {
                var count = 0;
                var currentNode = root;
                while (true)
                {
                    if (currentNode is null)
                        break;
                    count++;
                    currentNode = currentNode.Next;
                }
                return count;
            }
        }
        private class Node<T> 
        { 
            public Node<T> Next;
            public T Value;

            public Node(T value)
            {
                Value = value;
                Next = null;
            }
        }

        public void Push(T item)
        {
            var spin = new SpinWait();
            var node = new Node<T>(item);
            while (true)
            {
                var head = root;
                node.Next = head;
                if (Interlocked.CompareExchange(ref root, node, head) == head)
                    return;
                spin.SpinOnce();
            }
        }

        public bool TryPop(out T result)
        {
            result = default(T);
            var spin = new SpinWait();
            Node<T> head;
            while (true)
            {
                head = root;
                if (head == null)
                    return false;
                if (Interlocked.CompareExchange(ref root, head.Next, head) == head)
                {
                    result = head.Value;
                    return true;
                }
                spin.SpinOnce();
            }
        }
    }
    }
}