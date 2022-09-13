using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JenkinsNET;
using Extensions;
using ChatworkJobTrigger.Chatwork;

namespace ChatworkJobTrigger
{
    public sealed class WorkerManager : Singleton<WorkerManager>
    {
        //----- params -----
        
        //----- field -----

        private List<JobWorker> jobWorkers = null;

        //----- property -----

        //----- method -----
        
        protected override void OnCreate()
        {
            jobWorkers = new List<JobWorker>();
        }

        public void Update()
        {
            jobWorkers = jobWorkers.Where(x => x.Status != JenkinsJobStatus.Complete).ToList();
        }

        public JobWorker CreateNewWorker(MessageData triggerMessage)
        {
            var token = Guid.NewGuid().ToString("N").Substring(0, 8);

            var jobWorker = new JobWorker(token, triggerMessage);

            jobWorkers.Add(jobWorker);

            return jobWorker;
        }

        public JobWorker FindWorker(string token)
        {
            return jobWorkers.FirstOrDefault(x => x.Token == token);
        }
    }
}
