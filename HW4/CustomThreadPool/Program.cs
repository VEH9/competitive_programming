using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CustomThreadPool.ThreadPools;

namespace CustomThreadPool
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ThreadPoolTests.Run<CustomThreadPool>();
            ThreadPoolTests.Run<DotNetThreadPoolWrapper>();
        }
    }
    
    public class CustomThreadPool : IThreadPool, IDisposable
    {
        private readonly IReadOnlyList<Worker> workers;
        private long processedTask;
        private volatile int threadsWaitingCount;
        private readonly Queue<Action> globalQueue = new Queue<Action>();
        
        private CustomThreadPool(int concurrencyLevel)
        {
            if (concurrencyLevel <= 0)
                throw new ArgumentOutOfRangeException(nameof(concurrencyLevel));

            workers = Enumerable
                .Range(0, concurrencyLevel)
                .Select(x => Worker.CreateAndStart(WorkersLoop))
                .ToArray();
        }
        
        public CustomThreadPool() : this(Environment.ProcessorCount) {}

        public void EnqueueAction(Action action)
        {
            if (Worker.CurrentThreadIsWorker)
                Worker.GetCurrentThreadWorker().LocalQueue.LocalPush(action);
            else
                lock(globalQueue)
                    globalQueue.Enqueue(action);
            ResumeWorker();
        }

        public long GetTasksProcessedCount() => processedTask;

        private void WorkersLoop(Worker worker)
        {
            while (true)
            {
                GetTask().Invoke();
                Interlocked.Increment(ref processedTask);
            }

            Action GetTask()
            {
                if (TryGetTaskFromLocalQueue(out var task))
                    return task;
                while (true)
                {
                    if (TryGetTaskFromGlobalQueue(out task))
                        return task;
                    if (TryStealTask(out task))
                        return task;
                    WaitForNewTask();
                }
            }

            bool TryGetTaskFromLocalQueue(out Action task)
            {
                task = null;
                return worker.LocalQueue.LocalPop(ref task);
            }

            bool TryGetTaskFromGlobalQueue(out Action task)
            {
                lock (globalQueue)
                    return globalQueue.TryDequeue(out task);
            }

            bool TryStealTask(out Action task)
            {
                task = null;
                foreach (var anotherWorker in workers?.Where(w => w != worker) ?? Enumerable.Empty<Worker>())
                    if (anotherWorker.LocalQueue.TrySteal(ref task))
                        return true;
                return false;
            }
        }
        
        private void ResumeWorker()
        {
            if (threadsWaitingCount <= 0)
                return;
            
            lock(globalQueue)
                Monitor.Pulse(globalQueue);
        }

        private void WaitForNewTask()
        {
            lock (globalQueue)
            {
                threadsWaitingCount++;
                try
                {
                    Monitor.Wait(globalQueue);
                }
                finally
                {
                    threadsWaitingCount--;
                }
            }
        }

        public void Dispose()
        {
            foreach (var worker in workers)
                worker.Dispose();
        }
    }
    
    public class Worker : IDisposable
    {
        private static readonly ThreadLocal<Worker> CurrentWorker = new ThreadLocal<Worker>();
        public WorkStealingQueue<Action> LocalQueue { get; } = new WorkStealingQueue<Action>();
        private Thread Thread { get; }
        
        private Worker(Action<Worker> workerLoop)
        {
            Thread = new Thread(SetWorkerAndRunLoop) {IsBackground = true};
            void SetWorkerAndRunLoop()
            {
                CurrentWorker.Value = this;
                workerLoop(this);
            }
        }

        public static Worker CreateAndStart(Action<Worker> workerLoop)
        {
            var worker = new Worker(workerLoop);
            worker.Thread.Start();
            return worker;
        }

        public static bool CurrentThreadIsWorker => CurrentWorker.Value != null;

        public static Worker GetCurrentThreadWorker()
        {
            return CurrentThreadIsWorker
                ? CurrentWorker.Value
                : throw new InvalidOperationException("Current thread is not a worker");
        }
        
        public void Dispose() => CurrentWorker.Dispose();
    }
}