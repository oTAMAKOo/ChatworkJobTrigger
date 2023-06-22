
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensions;
using ChatworkJobTrigger.Chatwork;

namespace ChatworkJobTrigger
{
    public sealed class JobWorker
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        public string Token { get; private set; }

        public string JobName { get; private set; }

        public Dictionary<string, string> JobParameters { get; private set; }

        public int? QueuedNumber { get; private set; }

        public int? BuildNumber { get; private set; }

        public JobStatus Status { get; private set; }

        public MessageData TriggerMessage { get; private set; }

        //----- method -----

        public JobWorker(string token, MessageData triggerMessage)
        {
            Token = token;
            TriggerMessage = triggerMessage;
        }

        public async Task Invoke(Command command, string[] arguments, CancellationToken cancelToken)
        {
            var chatworkService = ChatworkService.Instance;

            try
            {
                var firstArgumentStr = arguments.ElementAtOrDefault(0, string.Empty).ToLower();

                switch (firstArgumentStr)
                {
                    case "help":
                        await HelpReqest(command, cancelToken);
                        break;

                    default:
                        await Build(command, arguments, cancelToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                // エラー通知.

                if (TriggerMessage != null)
                {
                    var message = chatworkService.GetReplyStr(TriggerMessage) + ex;

                    await chatworkService.SendMessage(message, cancelToken);
                }

                // ログ出力.

                ConsoleUtility.Separator();

                Console.WriteLine(ex);
                
                ConsoleUtility.Separator();
            }
        }

        public async Task Cancel(CancellationToken cancelToken)
        {
            var chatworkService = ChatworkService.Instance;
            var jenkinsService = JenkinsService.Instance;

            try
            {
                var result = await jenkinsService.ReqestCancel(JobName, QueuedNumber, BuildNumber);

                // ビルド中の場合はビルド側でキャンセルメッセージが発行される.
                if (result && !BuildNumber.HasValue)
                {
                    var textDefine = TextDefine.Instance;

                    var resultMessage = string.Empty;

                    resultMessage += chatworkService.GetReplyStr(TriggerMessage);
                    resultMessage += textDefine.JobCanceled;

                    await chatworkService.SendMessage(resultMessage, cancelToken);
                }
            }
            catch (Exception ex)
            {
                // エラー通知.

                if (TriggerMessage != null)
                {
                    var message = chatworkService.GetReplyStr(TriggerMessage) + ex;

                    await chatworkService.SendMessage(message, cancelToken);
                }

                // ログ出力.

                ConsoleUtility.Separator();

                Console.WriteLine(ex);
                
                ConsoleUtility.Separator();
            }
        }

        public async Task SendCurrentStatus(CancellationToken cancelToken)
        {
            var chatworkService = ChatworkService.Instance;
            var jenkinsService = JenkinsService.Instance;

            var replyStr = chatworkService.GetReplyStr(TriggerMessage);
            
            var jobStatusMessage = jenkinsService.GetJobStatusMessage(Status, BuildNumber);

            var message = replyStr + jobStatusMessage;

            await chatworkService.SendMessage(message, cancelToken);
        }

        private async Task HelpReqest(Command command, CancellationToken cancelToken)
        {
            var chatworkService = ChatworkService.Instance;

            var helpMessage = string.Empty;

            helpMessage += chatworkService.GetReplyStr(TriggerMessage);
            helpMessage += command.GetHelpText();

            await chatworkService.SendMessage(helpMessage, cancelToken);
        }

        private async Task Build(Command command, string[] arguments, CancellationToken cancelToken)
        {
            var chatworkService = ChatworkService.Instance;
            var jenkinsService = JenkinsService.Instance;
            
            await BuildJobData(command, arguments, cancelToken);

            var result = await jenkinsService.ReqestBuild(JobName, JobParameters, OnJobStatusChanged);

            var resultMessage = string.Empty;

            if (result.Error == null)
            {
                resultMessage += chatworkService.GetReplyStr(TriggerMessage);
                resultMessage += jenkinsService.GetJobMessage(result.Status, result.BuildNumber, Token);

                // ジョブ失敗時にはジョブのログファイルを送る.

                var filePath = string.Empty;

                if (result.Status == JobStatus.Failed || result.Status == JobStatus.Unknown)
                {
                    if (result.BuildNumber.HasValue)
                    {
                        var buildNumber = result.BuildNumber.Value;
                                
                        filePath = jenkinsService.GetLogFilePath(result.JobName, buildNumber);

                        if (!File.Exists(filePath))
                        {
                            filePath = null;
                        }
                    }
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    await chatworkService.SendMessage(resultMessage, cancelToken);
                }
                else
                {
                    await chatworkService.SendFile(filePath, resultMessage, "log.txt", cancelToken);
                }
            }
            else
            {
                resultMessage += chatworkService.GetReplyStr(TriggerMessage);
                resultMessage += "Jenkins job info get failed.";

                if (result.Error != null)
                {
                    resultMessage += $"\n{result.Error.Message}";

                    if (result.Error.InnerException != null)
                    {
                        resultMessage += $"\n{result.Error.InnerException.Message}";
                    }
                }

                await chatworkService.SendMessage(resultMessage, cancelToken);
            }
        }

        private async Task BuildJobData(Command command, string[] arguments, CancellationToken cancelToken)
        {
            var chatworkService = ChatworkService.Instance;

            JobName = string.Empty;
            JobParameters = new Dictionary<string, string>();

            try
            {
                JobParameters = GetJobParameters(command, arguments);

                ValidateJobParameters(command, JobParameters);

                JobName = GetJobName(command, JobParameters);
            }
            catch (Exception ex)
            {
                // コマンドが間違っている通知.

                if (TriggerMessage != null)
                {
                    var textDefine = TextDefine.Instance;

                    var message = chatworkService.GetReplyStr(TriggerMessage);
                    
                    if (!string.IsNullOrEmpty(textDefine.CommandError))
                    {
                        message += textDefine.CommandError + "\n";
                    }
                    
                    message += ex.Message;

                    await chatworkService.SendMessage(message, cancelToken);
                }
            }
        }

        private void ValidateJobParameters(Command command, Dictionary<string, string> jobParameters)
        {
            var textDefine = TextDefine.Instance;

            foreach (var argument in command.Arguments)
            {
                if (argument.Require)
                {
                    // 存在しない.

                    if (!jobParameters.ContainsKey(argument.Field))
                    {
                        var errorMessage = textDefine.ArgumentNotFoundError.Replace("#FIELD_NAME#", argument.Field);

                        throw new ArgumentException(errorMessage);
                    }

                    // 候補一覧に値が存在しない.

                    if (argument.ValuePattern.Any())
                    {
                        var value = jobParameters[argument.Field];
                        var valueStr = argument.ConvertValue(value);

                        if (string.IsNullOrEmpty(valueStr))
                        {
                            var errorMessage = textDefine.UndefinedValueError.Replace("#VALUE#", value);

                            throw new ArgumentException(errorMessage);
                        }
                    }
                }
            }
        }

        private string GetJobName(Command command, Dictionary<string, string> jobParameters)
        {
            var jobName = command.JobNameFormat;

            var fieldNames = command.Arguments.Select(x => x.Field).ToArray();

            foreach (var fieldName in fieldNames)
            {
                var value = jobParameters.GetValueOrDefault(fieldName);

                jobName = jobName.Replace($"#{fieldName.ToUpper()}#", value);
            }

            return jobName;
        }

        private Dictionary<string, string> GetJobParameters(Command command, string[] arguments)
        {
            var dictionary = new Dictionary<string, string>();

            var fieldNames = command.Arguments.Select(x => x.Field).ToArray();

            //----- 引数名:引数で順番無視で登録 -----

            foreach (var argument in arguments)
            {
                if (!argument.Contains(":")){ continue; }

                var lowerStr = argument.Trim().ToLower();
                
                foreach (var commandArgument in command.Arguments)
                {
                    var tag = commandArgument.Field.ToLower() + ":";

                    if (!lowerStr.StartsWith(tag)){ continue; }

                    if (!dictionary.ContainsKey(commandArgument.Field))
                    {
                        var temp = argument.Substring(tag.Length).Trim();

                        if (string.IsNullOrEmpty(temp))
                        {
                            temp = commandArgument.DefaultValue;
                        }

                        var valueStr = commandArgument.ConvertValue(temp);

                        var value = ConvertValue(commandArgument.Type, valueStr);

                        dictionary.Add(commandArgument.Field, value);
                    }
                }
            }

            //----- 通常登録 -----

            // 登録済みの引数名除外.

            fieldNames = fieldNames.Where(x => !dictionary.ContainsKey(x)).ToArray();
            arguments = arguments.Where(x => !x.Contains(":")).ToArray();

            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];

                var fieldName = fieldNames.ElementAtOrDefault(i);
                var commandArgument = command.Arguments.FirstOrDefault(x => x.Field == fieldName);

                if (!string.IsNullOrEmpty(fieldName) && !dictionary.ContainsKey(fieldName))
                {
                    var valueStr = commandArgument.ConvertValue(argument);
                    var value = ConvertValue(commandArgument.Type, valueStr);

                    dictionary.Add(fieldName, value);
                } 
            }

            //----- デフォルト引数 -----

            foreach (var commandArgument in command.Arguments)
            {
                var fieldName = commandArgument.Field;

                if (dictionary.ContainsKey(fieldName)){ continue; }

                if (commandArgument.Require){ continue; }

                var valueStr = commandArgument.DefaultValue;
                var value = ConvertValue(commandArgument.Type, valueStr);

                dictionary.Add(fieldName, value);
            }

            return dictionary;
        }
        
        private string ConvertValue(Type type, string value)
        {
            if (type == typeof(bool))
            {
                value = bool.Parse(value).ToString().ToLower();
            }

            if (type == typeof(int))
            {
                value = int.Parse(value).ToString();
            }

            return value;
        }

        private void OnJobStatusChanged(JobStatus jobStatus, int? queuedNumber, int? buildNumber)
        {
            Status = jobStatus;
            
            if (TriggerMessage == null){ return; }
         
            var chatworkService = ChatworkService.Instance;
            var jenkinsService = JenkinsService.Instance;
            
            var message = string.Empty;

            var replyStr = chatworkService.GetReplyStr(TriggerMessage);

            QueuedNumber = queuedNumber;
            BuildNumber = buildNumber;

            switch (jobStatus)
            {
                case JobStatus.Queued:
                    message = replyStr + jenkinsService.GetJobMessage(jobStatus, buildNumber, Token);
                    break;
            }

            if (!string.IsNullOrEmpty(message))
            {
                chatworkService.SendMessage(message, CancellationToken.None).Forget();
            }
        }
    }
}
