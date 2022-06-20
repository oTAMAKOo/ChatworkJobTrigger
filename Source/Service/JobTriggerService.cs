using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Extensions;
using ChatworkJenkinsBot.Chatwork;

namespace ChatworkJenkinsBot
{
    public sealed class JobTriggerInfo
    {
        /// <summary>  </summary>
        public AccountData Sender { get; private set; }
        /// <summary>  </summary>
        public string Command { get; private set; }
        /// <summary>  </summary>
        public string[] Arguments { get; private set; }

        public JobTriggerInfo(AccountData sender, string command, string[] arguments)
        {
            Sender = sender;
            Command = command;
            Arguments = arguments;
        }
    }

    public sealed class JobTriggerService : Singleton<JobTriggerService>
    {
        //----- params -----

        private const int FetchIntervalSeconds = 60;

        //----- field -----

        private IJobTrigger[] jobTriggers = null;

        private DateTime fetchTime = default;

        private DateTime nextFetchTime = default;

        //----- property -----

        //----- method -----

        private JobTriggerService() { }

        public async Task Initialize()
        {
            jobTriggers = new IJobTrigger[]
            {
                BuildJobTrigger.Instance,
                MasterJobTrigger.Instance,
                ResourceJobTrigger.Instance,
            };

            foreach (var jobTrigger in jobTriggers)
            {
                await jobTrigger.Initialize();
            }
        }

        public bool IsTriggerMessage(MessageData messageData)
        {
            var body = messageData.body;

            var triggerUserStr = GetTriggerUserStr();

            var triggerUserIndex = body.IndexOf(triggerUserStr, StringComparison.CurrentCulture);

            return triggerUserIndex != -1;
        }

        private string GetTriggerUserStr()
        {
            var chatworkService = ChatworkService.Instance;

            var myAccount = chatworkService.MyAccount;

            return $"[To:{myAccount.account_id}]{myAccount.name}";
        }

        public async Task Fetch(CancellationToken cancelToken)
        {
            var time = DateTime.Now;

            if (time < nextFetchTime){ return; }

            foreach (var jobTrigger in jobTriggers)
            {
                await jobTrigger.Fetch(cancelToken);
            }

            fetchTime = time;

            nextFetchTime = time.AddSeconds(FetchIntervalSeconds);
        }

        public async Task InvokeTrigger(MessageData message, CancellationToken cancelToken)
        {
            var triggerInfo = GetJobTriggerInfo(message);

            foreach (var jobTrigger in jobTriggers)
            {
                if(jobTrigger.CommandName != triggerInfo.Command){ continue; }

                jobTrigger.SetRequestMessageData(message);

                await jobTrigger.Invoke(triggerInfo.Arguments, cancelToken);
            }
        }

        private JobTriggerInfo GetJobTriggerInfo(MessageData messageData)
        {
            var body = messageData.body;

            var triggerUserStr = GetTriggerUserStr();

            var elements = body.Trim()
                .Replace("\n", " ")
                .Split(' ')
                .Where(x => !x.Contains(triggerUserStr))
                .ToArray();

            var command = elements.ElementAtOrDefault(0);
            var arguments = elements.Skip(1).ToArray();

            return new JobTriggerInfo(messageData.account, command, arguments);
        }
    }
}
