using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CustomThreadPool.ThreadPools
{
    public interface IThreadPool
    {
        void EnqueueAction(Action action);
        long GetTasksProcessedCount();
    }

    public class CustomThreadPoolWrapper : IThreadPool
    {
        private long processedTask = 0L;
        public void EnqueueAction(Action action)
        {
            CustomThreadPool.AddAction(delegate
            {
                action.Invoke();
                Interlocked.Increment(ref processedTask);
            });
        }

        public long GetTasksProcessedCount() => processedTask;
    }

    public static class CustomThreadPool
    {
        private static readonly Queue<Action> queue = new Queue<Action>();
        private static readonly Dictionary<int, WorkStealingQueue<Action>> actions = new Dictionary<int, WorkStealingQueue<Action>>();
        static CustomThreadPool()
        {
            RunBackgroundThreads(Worker, 16);
        }

        private static void Worker()
        {
            Action currentAction = null;
            while (true)
            {
                while (actions[Thread.CurrentThread.ManagedThreadId].LocalPop(ref currentAction))
                {
                    currentAction?.Invoke();
                }

                var isCurrentThreadQueueEmpty = TryDequeueAndFindFlag();

                if (isCurrentThreadQueueEmpty)
                {
                    isCurrentThreadQueueEmpty = TryStealActionPool(true);
                }

                if (isCurrentThreadQueueEmpty)
                {
                    TryDequeueElseWait();
                }
            }
        }

        private static bool TryDequeueAndFindFlag()
        {
            var isCurrentThreadQueueEmpty = false;
            lock (queue)
            {
                if (queue.TryDequeue(out var action))
                {
                    actions[Thread.CurrentThread.ManagedThreadId].LocalPush(action);
                }
                else
                {
                    isCurrentThreadQueueEmpty = true;
                }
            }
            return isCurrentThreadQueueEmpty;
        }

        private static bool TryStealActionPool(bool isCurrentThreadQueueEmpty)
        {
            Action action = delegate { };
            if (!actions.Any(threadPool => threadPool.Value.TrySteal(ref action))) return isCurrentThreadQueueEmpty;
            actions[Thread.CurrentThread.ManagedThreadId].LocalPush(action);
            return false;
        }

        private static void TryDequeueElseWait()
        {
            lock (queue)
            {
                if (queue.TryDequeue(out var action))
                    actions[Thread.CurrentThread.ManagedThreadId].LocalPush(action);
                else
                    Monitor.Wait(queue);
            }
        }

        public static void AddAction(Action action)
        {
            lock (queue)
            {
                queue.Enqueue(action);
                Monitor.Pulse(queue);
            }
        }

        private static Thread[] RunBackgroundThreads(Action action, int count)
        {
            var threads = new List<Thread>();
            for (var i = 0; i < count; i++)
                threads.Add(RunBackgroundThread(action));
            return threads.ToArray();
        }

        private static Thread RunBackgroundThread(Action action)
        {
            var thread = new Thread(() => action())
            {
                IsBackground = true
            };
            actions[thread.ManagedThreadId] = new WorkStealingQueue<Action>();
            thread.Start();
            return thread;
        }
    }
}