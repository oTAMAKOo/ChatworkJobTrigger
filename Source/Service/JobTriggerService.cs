
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensions;
using ChatworkJobTrigger.Chatwork;

namespace ChatworkJobTrigger
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
        
        //----- field -----

        //----- property -----

        //----- method -----

        private JobTriggerService() { }

        public async Task Initialize()
        {
            var jobTrigger = JobTrigger.Instance;

            await jobTrigger.Initialize();
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

        public async Task InvokeTrigger(MessageData message, CancellationToken cancelToken)
        {
            var jobTrigger = JobTrigger.Instance;

            var triggerInfo = GetJobTriggerInfo(message);

            foreach (var command in jobTrigger.Commands)
            {
                if(command.CommandName != triggerInfo.Command){ continue; }

                jobTrigger.SetRequestMessageData(message);

                await jobTrigger.Invoke(command, triggerInfo.Arguments, cancelToken);
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
