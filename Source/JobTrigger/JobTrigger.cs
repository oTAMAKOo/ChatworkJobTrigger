
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JenkinsNET;
using Extensions;
using ChatworkJobTrigger.Chatwork;

namespace ChatworkJobTrigger
{
    public sealed class JobTrigger : Singleton<JobTrigger>
    {
        //----- params -----

        //----- field -----
        
        private MessageData requestMessage = null;

        //----- property -----

        public Command[] Commands { get; private set; }

        //----- method -----

        public async Task Initialize()
        {
            var setting = Setting.Instance;
            var commandFileLoader = CommandFileLoader.Instance;

            var commandList = new List<Command>();

            var commandNames = setting.Commands.Split(',').Select(x => x.Trim()).ToArray();

            foreach (var commandName in commandNames)
            {
                var command = await commandFileLoader.Load(commandName);

                commandList.Add(command);
            }

            Commands = commandList.ToArray();
        }

        public void SetRequestMessageData(MessageData requestMessage)
        {
            this.requestMessage = requestMessage;
        }

        public async Task Invoke(Command command, string[] arguments, CancellationToken cancelToken)
        {
            var chatworkService = ChatworkService.Instance;
            var jenkinsService = JenkinsService.Instance;

            JobInfo jobInfo = null;

            try
            {
                if (arguments.FirstOrDefault().ToLower() == "help")
                {
                    var helpMessage = string.Empty;

                    helpMessage += chatworkService.GetReplyStr(requestMessage);
                    helpMessage += command.GetHelpText();

                    await chatworkService.SendMessage(helpMessage, cancelToken);
                }
                else
                {
                    var jobData = await BuildJobData(command, arguments, cancelToken);

                    if (jobData == null){ return; }

                    jobInfo = await jenkinsService.RunJenkinsJob(jobData.Item1, jobData.Item2, OnJobStatusChanged);

                    if (jobInfo != null)
                    {
                        var resultMessage = string.Empty;

                        resultMessage += chatworkService.GetReplyStr(requestMessage);
                        resultMessage += jenkinsService.GetJobResultMessage(jobInfo);

                        // ジョブ失敗時にはジョブのログファイルを送る.

                        var filePath = string.Empty;

                        if (jobInfo.Status == JobStatus.Failed)
                        {
                            if (jobInfo.ResultInfo != null && jobInfo.ResultInfo.Number.HasValue)
                            {
                                var buildNumber = jobInfo.ResultInfo.Number.Value;
                                
                                filePath = jenkinsService.GetLogFilePath(jobInfo.JobName, buildNumber);

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
                }
            }
            catch (Exception ex)
            {
                // エラー通知.

                if (requestMessage != null)
                {
                    var message = chatworkService.GetReplyStr(requestMessage) + "\n" + ex;

                    await chatworkService.SendMessage(message, cancelToken);
                }

                // ログ出力.

                ConsoleUtility.Separator();

                Console.WriteLine(ex);
                
                ConsoleUtility.Separator();
            }
        }

        private async Task<Tuple<string, Dictionary<string, string>>> BuildJobData(Command command, string[] arguments, CancellationToken cancelToken)
        {
            var chatworkService = ChatworkService.Instance;

            var jobName = string.Empty;
            var jobParameters = new Dictionary<string, string>();

            try
            {
                jobParameters = GetJobParameters(command, arguments);

                ValidateJobParameters(command, jobParameters);

                jobName = GetJobName(command, jobParameters);
            }
            catch (Exception ex)
            {
                // コマンドが間違っている通知.

                if (requestMessage != null)
                {
                    var textDefine = TextDefine.Instance;

                    var message = chatworkService.GetReplyStr(requestMessage);
                    
                    if (!string.IsNullOrEmpty(textDefine.CommandError))
                    {
                        message += textDefine.CommandError + "\n";
                    }
                    
                    message += ex.Message;

                    await chatworkService.SendMessage(message, cancelToken);
                }

                return null;
            }

            return Tuple.Create(jobName, jobParameters);
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
                        throw new ArgumentException(string.Format(textDefine.ArgumentNotFoundError, argument.Field));
                    }

                    // 候補一覧に値が存在しない.

                    if (argument.ValuePattern.Any())
                    {
                        var value = jobParameters[argument.Field];
                        var valueStr = argument.ConvertValue(value);

                        if (string.IsNullOrEmpty(valueStr))
                        {
                            throw new ArgumentException(string.Format(textDefine.UndefinedValueError, value));
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

        private void OnJobStatusChanged(JenkinsJobStatus jobStatus)
        {
            var chatworkService = ChatworkService.Instance;

            if (requestMessage != null)
            {
                var textDefine = TextDefine.Instance;

                var message = string.Empty;

                switch (jobStatus)
                {
                    case JenkinsJobStatus.Queued:
                        message = chatworkService.GetReplyStr(requestMessage) + textDefine.JobQueued;
                        break;
                }

                if (!string.IsNullOrEmpty(message))
                {
                    chatworkService.SendMessage(message, CancellationToken.None).Forget();
                }
            }
        }
    }
}
