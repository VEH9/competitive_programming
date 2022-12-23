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
}