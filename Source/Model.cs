using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Extensions;

namespace ChatworkJenkinsBot
{
    public sealed class Model : Singleton<Model>
    {
        //----- params -----

        //----- field -----
        
        /// <summary> [コマンド, コマンド引数名配列]の辞書. </summary>
        private Dictionary<string, string[]> argumentNames = null;

        /// <summary> [コマンド引数名, 引数候補配列]の辞書. </summary>
        private Dictionary<string, string[]> argumentCandidates = null;
        
        /// <summary> [コマンド, ジョブ名フォーマット]の辞書. </summary>
        private Dictionary<string, string> jobNames = null;

        //----- property -----

        //----- method -----

        protected override void OnCreate()
        {
            argumentNames = new Dictionary<string, string[]>();
            argumentCandidates = new Dictionary<string, string[]>();
            jobNames = new Dictionary<string, string>();
        }

        // 引数名

        public void SetArgumentNames(string command, string[] names)
        {
            var items = names.Where(x => !string.IsNullOrEmpty(x))
                .Where(x => !x.StartsWith("//"))
                .ToArray();

            argumentNames[command] = items;
        }

        public string[] GetArgumentNames(string command)
        {
            return argumentNames.GetValueOrDefault(command);
        }

        // 引数候補.

        public void SetCandidates(string commandArgumentName, string[] candidates)
        {
            var items = candidates.Where(x => !string.IsNullOrEmpty(x))
                .Where(x => !x.StartsWith("//"))
                .ToArray();

            argumentCandidates[commandArgumentName] = items;
        }

        public string[][] GetCandidates(string commandArgumentName)
        {
            var list = new List<string[]>();

            var candidates = argumentCandidates.GetValueOrDefault(commandArgumentName);
            
            if (candidates == null){ return null; }

            foreach (var candidate in candidates)
            {
                var items = candidate.Replace("\n", string.Empty)
                    .Split(',')
                    .Select(x => x.Trim())
                    .ToArray();

                list.Add(items);
            }

            return list.ToArray();
        }

        // ジョブ名.

        public void SetJobName(string command, string jobName)
        {
            if (jobName.StartsWith("//")){ return; }

            jobNames[command] = jobName;
        }

        public string GetJobName(string command, string[] arguments)
        {
            var jobName = jobNames.GetValueOrDefault(command);

            var jobArguments = GetJobArgument(command, arguments);

            // 置き換えシンボルが定義されている場合は置き換え.

            if (jobName.Contains("#"))
            {
                foreach (var jobArgument in jobArguments)
                {
                    var argumentStr = jobArgument.Value.ToLower();

                    var argumentCandidate = argumentCandidates.GetValueOrDefault(jobArgument.Key);

                    if (argumentCandidate != null)
                    {
                        var symbol = $"#{jobArgument.Key.ToUpper()}#";

                        var replaceStr = string.Empty;

                        if (jobName.Contains(symbol))
                        {
                            // 定義一覧の最初の定義に置き換え.

                            var candidates = GetCandidates(jobArgument.Key);

                            foreach (var candidate in candidates)
                            {
                                if (candidate.Any(x => x.ToLower() == argumentStr))
                                {
                                    replaceStr = candidate.First().ToLower();
                                }
                            }

                            // 置き換え.

                            if (!string.IsNullOrEmpty(replaceStr))
                            {
                                jobName = jobName.Replace(symbol, replaceStr);
                            }
                            else
                            {
                                throw new InvalidDataException($"Symbol not found. {symbol} : [{jobArgument.Key}, {jobArgument.Value}]");
                            }
                        }
                    }
                }
            }

            return jobName;
        }

        // ジョブ引数.

        public Dictionary<string, string> GetJobArgument(string command, string[] arguments)
        {
            var jobArgument = new Dictionary<string, string>();
            
            var argNames = GetArgumentNames(command);

            //----- 引数名:引数で順番無視で登録 -----

            foreach (var argument in arguments)
            {
                if (!argument.Contains(":")){ continue; }
                
                foreach (var argumentName in argNames)
                {
                    var tag = argumentName.Trim() + ":";

                    if (!argument.StartsWith(tag)){ continue; }

                    if (!jobArgument.ContainsKey(argumentName))
                    {
                        var value = argument.Substring(tag.Length);

                        jobArgument.Add(argumentName, value);
                    }
                }
            }

            //----- 通常登録 -----

            // 登録済みの引数名除外.
            argNames = argNames.Where(x => !jobArgument.ContainsKey(x)).ToArray();

            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];

                if (argument.Contains(":")){ continue; }

                var argumentName = argNames.ElementAtOrDefault(i);

                if (!string.IsNullOrEmpty(argumentName) && !jobArgument.ContainsKey(argumentName))
                {
                    jobArgument.Add(argumentName, argument);
                } 
            }
            
            return jobArgument;
        }
    }
}
