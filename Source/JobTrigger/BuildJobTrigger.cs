
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatworkJenkinsBot
{
    public sealed class BuildJobTrigger : JobTrigger<BuildJobTrigger>
    {
        //----- params -----

        private const string ArgType = "Type";
        private const string ArgPlatform = "Platform";
        private const string ArgBranch = "Branch";
        private const string ArgFpsHide = "FpsHide";
        private const string ArgNoSRDebugger = "NoSRDebugger";

        //----- field -----

        private Dictionary<string, string[]> typeDictionary = null;
        private Dictionary<string, string[]> platformDictionary = null;

        //----- property -----

        public override string CommandName { get { return "build"; } }

        //----- method -----

        public override Task Initialize()
        {
            var jobTriggerConfig = JobTriggerConfig.Instance;

            typeDictionary = ParsePatternStr(jobTriggerConfig.BuildType);
            platformDictionary = ParsePatternStr(jobTriggerConfig.BuildPlatform);

            return Task.CompletedTask;
        }

        protected override string GetJobName(string[] arguments)
        {
            var jobTriggerConfig = JobTriggerConfig.Instance;

            var jobParameters = GetJobParameters(arguments);

            var jobNameFormat = jobTriggerConfig.BuildJobNameFormat;

            var type = jobParameters.GetValueOrDefault(ArgType);
            var platform = jobParameters.GetValueOrDefault(ArgPlatform);

            return jobNameFormat
                .Replace("#TYPE#", type)
                .Replace("#PLATFORM#", platform);
        }

        protected override Dictionary<string, string> GetJobParameters(string[] arguments)
        {
            var argumentNames = new string[]
            {
                ArgType, ArgPlatform, ArgBranch,
            };

            var dictionary = ParseArguments(argumentNames, arguments);

            if (dictionary.ContainsKey(ArgType))
            {
                dictionary[ArgType] = ReplacePatternStr(dictionary[ArgType], typeDictionary);

                if (typeDictionary.Keys.All(x => x != dictionary[ArgType]))
                {
                    throw new InvalidDataException($"Unknown type {dictionary[ArgType]}");
                }
            }

            if (dictionary.ContainsKey(ArgPlatform))
            {
                dictionary[ArgPlatform] = ReplacePatternStr(dictionary[ArgPlatform], platformDictionary);

                if (platformDictionary.Keys.All(x => x != dictionary[ArgPlatform]))
                {
                    throw new InvalidDataException($"Unknown platform {dictionary[ArgPlatform]}");
                }
            }

            // 引数に存在したらtrue.
            if (arguments.Any(x => x.ToLower() == ArgFpsHide.ToLower()))
            {
                dictionary[ArgFpsHide] = "true";
            }
            
            // 引数に存在したらtrue.
            if (arguments.Any(x => x.ToLower() == ArgNoSRDebugger.ToLower()))
            {
                dictionary[ArgNoSRDebugger] = "true";
            }

            return dictionary;
        }

        protected override string GetHelpText()
        {
            var textDefine = TextDefine.Instance;
            var jobTriggerConfig = JobTriggerConfig.Instance;

            var builder = new StringBuilder();

            builder.Append("[info][title]Build Help[/title]");
            builder.AppendLine("Format : build [type] [platform] [branch]");
            builder.AppendLine();
            builder.AppendLine($"type = {jobTriggerConfig.BuildType}");
            builder.AppendLine($"platform = {jobTriggerConfig.BuildPlatform}");
            builder.AppendLine();

            var helpMessage = textDefine.BuildHelp;

            if (!string.IsNullOrEmpty(helpMessage))
            {
                builder.Append(helpMessage).AppendLine();
            }

            builder.AppendLine("[/info]");

            return builder.ToString();
        }
    }
}
