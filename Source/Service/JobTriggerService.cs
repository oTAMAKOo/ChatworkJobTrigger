
using System;
using System.Collections.Generic;
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

        private Command[] commands = null;

        //----- property -----

        //----- method -----

        private JobTriggerService() { }

        public async Task Initialize()
        {
            var setting = Setting.Instance;

            // 実行コマンド情報構築.
            var commandFileLoader = CommandFileLoader.Instance;

            var commandList = new List<Command>();

            var commandNames = setting.Commands.Split(',').Select(x => x.Trim()).ToArray();

            foreach (var commandName in commandNames)
            {
                var command = await commandFileLoader.Load(commandName);

                commandList.Add(command);
            }

            commands = commandList.ToArray();
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
            var triggerInfo = GetJobTriggerInfo(message);

            if (triggerInfo.Command == "cancel")
            {
                await ExecuteCancel(triggerInfo, cancelToken);
            }
            else
            {
                var command = commands.FirstOrDefault(x => x.CommandName == triggerInfo.Command);
                
                await ExecuteCommand(command, triggerInfo, message, cancelToken);
            }
        }

        private async Task ExecuteCommand(Command command, JobTriggerInfo triggerInfo, MessageData message, CancellationToken cancelToken)
        {
            var workerManager = WorkerManager.Instance;

            workerManager.Update();

            if (command == null){ return; }

            var jobWorker = workerManager.CreateNewWorker(message);
            
            await jobWorker.Invoke(command, triggerInfo.Arguments, cancelToken);
        }

        private async Task ExecuteCancel(JobTriggerInfo triggerInfo, CancellationToken cancelToken)
        {
            var token = triggerInfo.Arguments.ElementAtOrDefault(0);

            if (string.IsNullOrEmpty(token)){ return; }
            
            var workerManager = WorkerManager.Instance;

            var jobWorker = workerManager.FindWorker(token);
            
            if (jobWorker != null)
            {
                await jobWorker.Cancel(cancelToken);
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
