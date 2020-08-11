using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System;

namespace Janphe
{

    public class ThreadedTaskQueue : IDisposable
    {

        Queue<System.Action> taskQueue = new Queue<System.Action>();
        List<Thread> workers = new List<Thread>();
        EventWaitHandle wait = new EventWaitHandle(false, EventResetMode.AutoReset);

        Action<string> debug;
        public void OnLog(Action<string> d) { debug = d; }

        public ThreadedTaskQueue(int workerCount)
        {
            for (var i = 0; i < workerCount; i++)
            {
                var w = new Thread(Worker);
                w.Start();
                workers.Add(w);
            }
        }

        void Worker()
        {
            try
            {
                while (true)
                {
                    wait.WaitOne();
                    var task = PopTask();
                    if (task == null)
                        continue;
                    try
                    {
                        task();
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        if (debug != null) debug.Invoke(e.Message);
                    }
                }
            }
            catch (ThreadAbortException e)
            {
                if (debug != null) debug.Invoke(e.Message);
            }
        }

        public void PushTask(System.Action task)
        {
            lock (taskQueue)
            {
                taskQueue.Enqueue(task);
            }
            wait.Set();
        }

        public System.Action PopTask()
        {
            lock (taskQueue)
            {
                if (taskQueue.Count > 0)
                {
                    var task = taskQueue.Dequeue();
                    wait.Set();
                    return task;
                }
            }
            return null;
        }

        public void Dispose()
        {
            foreach (var w in workers)
            {
                w.Abort();
            }
            workers.Clear();
        }

    }
}
