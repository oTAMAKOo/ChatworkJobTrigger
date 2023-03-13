
using System;
using System.Linq;
using Extensions;
using ChatworkJobTrigger.Chatwork;

namespace ChatworkJobTrigger
{
    public sealed class WorkerManager : Singleton<WorkerManager>
    {
        //----- params -----
        
        //----- field -----

        private FixedQueue<JobWorker> jobWorkers = null;

        //----- property -----

        //----- method -----
        
        protected override void OnCreate()
        {
            jobWorkers = new FixedQueue<JobWorker>(1024);
        }
        
        public JobWorker CreateNewWorker(MessageData triggerMessage)
        {
            var token = Guid.NewGuid().ToString("N").Substring(0, 8);

            var jobWorker = new JobWorker(token, triggerMessage);

            jobWorkers.Enqueue(jobWorker);

            return jobWorker;
        }

        public JobWorker FindWorker(string token)
        {
            return jobWorkers.FirstOrDefault(x => x.Token == token);
        }
    }
}
