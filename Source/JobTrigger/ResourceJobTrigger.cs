
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;

namespace ChatworkJenkinsBot
{
    public sealed class ResourceJobTrigger : JobTrigger<ResourceJobTrigger>
    {
        //----- params -----

        private const string ArgServer = "Server";
        private const string ArgPlatform = "Platform";
        private const string ArgBranch = "Branch";

        //----- field -----

        private Dictionary<string, string[]> serverDictionary = null;
        private Dictionary<string, string[]> platformDictionary = null;

        //----- property -----

        public override string CommandName { get { return "resource"; } }

        //----- method -----

        public override Task Initialize()
        {
            var jobTriggerConfig = JobTriggerConfig.Instance;

            serverDictionary = ParsePatternStr(jobTriggerConfig.ResourceServer);
            platformDictionary = ParsePatternStr(jobTriggerConfig.ResourcePlatform);

            return Task.CompletedTask;
        }

        protected override string GetJobName(string[] arguments)
        {
            var jobTriggerConfig = JobTriggerConfig.Instance;

            var jobParameters = GetJobParameters(arguments);

            var jobNameFormat = jobTriggerConfig.ResourceJobNameFormat;
            
            var platform = jobParameters.GetValueOrDefault(ArgPlatform);

            return jobNameFormat.Replace("#PLATFORM#", platform);
        }

        protected override Dictionary<string, string> GetJobParameters(string[] arguments)
        {
            var argumentNames = new string[]
            {
                ArgServer, ArgPlatform, ArgBranch,
            };

            var dictionary = ParseArguments(argumentNames, arguments);

            if (dictionary.ContainsKey(ArgServer))
            {
                dictionary[ArgServer] = ReplacePatternStr(dictionary[ArgServer], serverDictionary);

                if (serverDictionary.Keys.All(x => x != dictionary[ArgServer]))
                {
                    throw new InvalidDataException($"Unknown server {dictionary[ArgServer]}");
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

            return dictionary;
        }

        protected override string GetHelpText()
        {
            var textDefine = TextDefine.Instance;
            var jobTriggerConfig = JobTriggerConfig.Instance;

            var builder = new StringBuilder();

            builder.Append("[info][title]Resource Help[/title]");
            builder.AppendLine("Format : resource [server] [platform] [branch]");
            builder.AppendLine();
            builder.AppendLine($"server = {jobTriggerConfig.ResourceServer}");
            builder.AppendLine($"platform = {jobTriggerConfig.ResourcePlatform}");
            builder.AppendLine();

            var helpMessage = textDefine.ResourceHelp;

            if (!string.IsNullOrEmpty(helpMessage))
            {
                builder.Append(helpMessage).AppendLine();
            }

            builder.AppendLine("[/info]");

            return builder.ToString();
        }
    }
}
