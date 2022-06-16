﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Extensions;

namespace ChatworkJenkinsBot
{
    public sealed class ChatworkService : Singleton<ChatworkService>
    {
        //----- params -----

        private const int FetchIntervalSeconds = 15;

        private static readonly DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        //----- field -----
        
        private ChatworkClient client = null;
        
        private ChatworkClient.AccountData myAccount = null;

        private DateTime fetchTime = default;

        private DateTime nextFetchTime = default;

        //----- property -----

        //----- method -----

        private ChatworkService(){ }

        public async Task Initialize(CancellationToken cancelToken)
        {
            Console.WriteLine("ChatWorkService");

            var config = ChatworkConfig.Instance;

            await config.Load();

            client = new ChatworkClient(config.RoomId, config.ApiKey);

            await GetMyAccountData(cancelToken);

            fetchTime = DateTime.Now;
            nextFetchTime = fetchTime.AddSeconds(FetchIntervalSeconds);
        }

        public async Task GetMyAccountData(CancellationToken cancelToken)
        {
            var json = await client.GetMyAccount(cancelToken);

            myAccount = JsonConvert.DeserializeObject<ChatworkClient.AccountData>(json);
        }

        public async Task Fetch(CancellationToken cancelToken)
        {
            var time = DateTime.Now;

            if (time < nextFetchTime){ return; }

            var json = await client.GetMessage(cancelToken);

            if (!string.IsNullOrEmpty(json))
            {
                var unixTime = (long)fetchTime.ToUniversalTime().Subtract(UNIX_EPOCH).TotalSeconds;

                await ParseMessages(json, unixTime, cancelToken);
            }

            fetchTime = time;
            nextFetchTime = time.AddSeconds(FetchIntervalSeconds);
        }

        private async Task ParseMessages(string json, long unixTime, CancellationToken cancelToken)
        {
            var messageDatas = JsonConvert.DeserializeObject<ChatworkClient.MessageData[]>(json);
            
            // 前回取得後以降に投稿・更新された対象.
            var messages = messageDatas.Where(x =>　unixTime < x.send_time).ToArray();

            foreach (var message in messages)
            {
                try
                {
                    var jobInfo = await ParseMessage(message, cancelToken);

                    if (jobInfo != null)
                    {
                        //RequestJenkinsJob(jobInfo.Item1, jobInfo.Item2, message, cancelToken).Forget();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                await Task.Delay(1, cancelToken);
            }

            await Task.Delay(1, cancelToken);
        }

        private async Task<Tuple<string, Dictionary<string, string>>> ParseMessage(ChatworkClient.MessageData messageData, CancellationToken cancelToken)
        {
            var model = Model.Instance;

            // ジョブをトリガーするユーザー判定.

            var triggerUserStr = $"[To:{myAccount.account_id}]{myAccount.name}";

            var triggerUserIndex = messageData.body.IndexOf(triggerUserStr, StringComparison.CurrentCulture);

            if (triggerUserIndex == -1){ return null; }
            
            var elements = messageData.body.Trim()
                .Replace("\n", " ")
                .Split(' ')
                .Where(x => !x.Contains(triggerUserStr))
                .ToArray();

            // ジョブ情報生成.

            Tuple<string, Dictionary<string, string>> jobInfo = null;

            try
            {
                var command = elements.ElementAtOrDefault(0);
                var arguments = elements.Skip(1).ToArray();

                var jobName = model.GetJobName(command, arguments);
                var jobArgument = model.GetJobArgument(command, arguments);

                jobInfo = Tuple.Create(jobName, jobArgument);

                ConsoleUtility.Separator();

                var items = jobInfo.Item2.Select(kvp => kvp.ToString());
                var args = string.Join(", ", items);

                Console.WriteLine($"JobName: {jobInfo.Item1}\nArguments: {args}");

                ConsoleUtility.Separator();
            }
            catch (Exception ex)
            {
                // コマンドが間違っている通知.

                var chatworkConfig = ChatworkConfig.Instance;
                var messageConfig = MessageConfig.Instance;

                var message = $"[rp aid={messageData.account.account_id} to={chatworkConfig.RoomId}-{messageData.message_id}]" +
                              messageConfig.RequestCommandError;
                //TODO:
                //await client.SendMessage(message, cancelToken);

                // ログ出力.

                ConsoleUtility.Separator();

                Console.WriteLine(ex.Message);
                
                ConsoleUtility.Separator();
            }

            return jobInfo;
        }

        private async Task RequestJenkinsJob(string jobName, Dictionary<string, string> jobArguments, ChatworkClient.MessageData messageData, CancellationToken cancelToken)
        {
            var chatworkConfig = ChatworkConfig.Instance;
            var messageConfig = MessageConfig.Instance;

            var jenkinsService = JenkinsService.Instance;
            
            var jobInfo = await jenkinsService.RunJenkinsJob(jobName, jobArguments);

            var message = $"[rp aid={messageData.account.account_id} to={chatworkConfig.RoomId}-{messageData.message_id}]\n";

            switch (jobInfo.Result)
            {
                case JobResult.Success:
                    message += messageConfig.JobSuccess;
                    break;
                case JobResult.Failed:
                    message += messageConfig.JobFailed;
                    break;
                case JobResult.Canceled:
                    message += messageConfig.JobCanceled;
                    break;
            }

            message = message.Replace("#BUILD_NUMBER#", jobInfo.ResultInfo.Number.ToString());

            await client.SendMessage(message, cancelToken);
        }
    }
}
