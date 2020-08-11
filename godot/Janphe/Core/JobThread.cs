using System;
using System.Threading;

namespace Janphe
{
    public class JobThread
    {
        protected bool working { get; private set; }
        protected bool waitJob { get; private set; }

        public void processAsync(Action<long> callback)
        {
            if (working)
            {
                waitJob = true;
                return;
            }

            working = true;
            waitJob = false;

            new Thread(new ThreadStart(() =>
            {
                process(t =>
                {
                    callback(t);
                    working = false;
                    if (waitJob)
                    {
                        beforeNextJob();
                        processAsync(callback);
                    }
                });
            })).Start();
        }

        protected virtual void beforeNextJob()
        {

        }

        protected virtual void process(Action<long> callback)
        {
            throw new NotImplementedException();
        }
    }
}
