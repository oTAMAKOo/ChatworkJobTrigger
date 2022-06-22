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
    public interface IJobTrigger
    {
        string CommandName { get; }

        Task Initialize();

        void SetRequestMessageData(MessageData requestMessage);

        Task Fetch(CancellationToken cancelToken);

        Task<JobInfo> Invoke(string[] arguments, CancellationToken cancelToken);
    }

    public abstract class JobTrigger<TInstance> : Singleton<TInstance> , IJobTrigger where TInstance : JobTrigger<TInstance>
    {
        //----- params -----

        //----- field -----

        protected MessageData requestMessage = null;

        //----- property -----

        public abstract string CommandName { get; }

        //----- method -----

        public virtual Task Initialize()
        {
            return Task.CompletedTask;
        }

        public void SetRequestMessageData(MessageData requestMessage)
        {
            this.requestMessage = requestMessage;
        }

        public virtual Task Fetch(CancellationToken cancelToken)
        {
            return Task.CompletedTask;
        }

        public async Task<JobInfo> Invoke(string[] arguments, CancellationToken cancelToken)
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
                    helpMessage += GetHelpText();

                    await chatworkService.SendMessage(helpMessage, cancelToken);
                }
                else
                {
                    var jobName = GetJobName(arguments);
                    var jobParameters = GetJobParameters(arguments);

                    jobInfo = await jenkinsService.RunJenkinsJob(jobName, jobParameters, OnJobStatusChanged);

                    if (jobInfo != null)
                    {
                        var resultMessage = string.Empty;

                        resultMessage += chatworkService.GetReplyStr(requestMessage);
                        resultMessage += jenkinsService.GetJobResultMessage(jobInfo);

                        await chatworkService.SendMessage(resultMessage, cancelToken);
                    }
                }
            }
            catch (InvalidDataException ex)
            {
                // コマンドが間違っている通知.

                if (requestMessage != null)
                {
                    var textDefine = TextDefine.Instance;

                    var message = chatworkService.GetReplyStr(requestMessage) + textDefine.CommandError + $"\n{ex.Message}";

                    await chatworkService.SendMessage(message, cancelToken);
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

            return jobInfo;
        } 
        
        protected Dictionary<string, string> ParseArguments(string[] argumentNames, string[] arguments)
        {
            var dictionary = new Dictionary<string, string>();

            //----- 引数名:引数で順番無視で登録 -----

            foreach (var argument in arguments)
            {
                if (!argument.Contains(":")){ continue; }
                
                foreach (var argumentName in argumentNames)
                {
                    var tag = argumentName.ToLower().Trim() + ":";

                    if (!argument.ToLower().StartsWith(tag)){ continue; }

                    if (!dictionary.ContainsKey(argumentName))
                    {
                        var value = argument.Substring(tag.Length);

                        dictionary.Add(argumentName, value);
                    }
                }
            }

            //----- 通常登録 -----

            // 登録済みの引数名除外.

            argumentNames = argumentNames.Where(x => !dictionary.ContainsKey(x)).ToArray();
            arguments = arguments.Where(x => !x.Contains(":")).ToArray();

            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];

                var argumentName = argumentNames.ElementAtOrDefault(i);

                if (!string.IsNullOrEmpty(argumentName) && !dictionary.ContainsKey(argumentName))
                {
                    dictionary.Add(argumentName, argument);
                } 
            }

            return dictionary;
        }

        protected Dictionary<string, string[]> ParsePatternStr(string target)
        {
            var dictionary = new Dictionary<string, string[]>();

            var patternStrs = new List<string>();

            var inPattern = false;
            var buffer = string.Empty;

            foreach (var str in target)
            {
                if (str == '[')
                {
                    inPattern = true;
                    buffer = string.Empty;
                }

                if (inPattern)
                {
                    buffer += str;

                    if (str == ']')
                    {
                        if (!string.IsNullOrEmpty(buffer))
                        {
                            patternStrs.Add(buffer.Trim());
                        }

                        inPattern = false;
                        buffer = string.Empty;
                    }
                }
            }

            var patternTable = new Dictionary<string, string>();

            for (var i = 0; i < patternStrs.Count; i++)
            {
                var pattern = patternStrs[i];

                var tempStr = $"###{i}###";
                
                target = target.Replace(pattern, tempStr);

                patternTable.Add(tempStr, pattern);
            }

            var elements = target.Split(',');

            foreach (var element in elements)
            {
                var str = element;
                var patterns = new string[0];

                foreach (var item in patternTable)
                {
                    if (element.Contains(item.Key))
                    {
                        str = str.Replace(item.Key, string.Empty);

                        patterns = item.Value.Substring(1, item.Value.Length - 2)
                            .Split(',')
                            .Select(x => x.Trim())
                            .ToArray();
                    }
                }

                dictionary.Add(str.Trim(), patterns);
            }

            return dictionary;
        }

        protected string ReplacePatternStr(string str, Dictionary<string, string[]> patterns)
        {
            foreach (var item in patterns)
            {
                foreach (var pattern in item.Value)
                {
                    if (pattern == str)
                    {
                        return item.Key;
                    }
                }
            }

            return str;
        }

        public virtual void OnJobStatusChanged(JenkinsJobStatus jobStatus)
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

        protected abstract string GetJobName(string[] arguments);

        protected abstract Dictionary<string, string> GetJobParameters(string[] arguments);

        protected abstract string GetHelpText();
    }
}
