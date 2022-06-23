
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;

namespace ChatworkJobTrigger
{
    public sealed class MasterJobTrigger : JobTrigger<MasterJobTrigger>
    {
        //----- params -----

        private const string ArgServer = "Server";
        private const string ArgBranch = "Branch";

        //----- field -----

        private Dictionary<string, string[]> serverDictionary = null;

        //----- property -----

        public override string CommandName { get { return "master"; } }

        //----- method -----

        public override Task Initialize()
        {
            var jobTriggerConfig = JobTriggerConfig.Instance;

            serverDictionary = ParsePatternStr(jobTriggerConfig.MasterServer);

            return Task.CompletedTask;
        }

        protected override string GetJobName(string[] arguments)
        {
            var jobTriggerConfig = JobTriggerConfig.Instance;

            var jobNameFormat = jobTriggerConfig.MasterJobNameFormat;

            return jobNameFormat;
        }

        protected override Dictionary<string, string> GetJobParameters(string[] arguments)
        {
            var argumentNames = new string[]
            {
                ArgServer, ArgBranch,
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

            return dictionary;
        }

        protected override string GetHelpText()
        {
            var textDefine = TextDefine.Instance;
            var jobTriggerConfig = JobTriggerConfig.Instance;

            var builder = new StringBuilder();
            
            builder.Append("[info][title]Master Help[/title]");
            builder.AppendLine("Format : master [server] [branch]");
            builder.AppendLine();
            builder.AppendLine($"server = {jobTriggerConfig.MasterServer}");

            var helpMessage = textDefine.MasterHelp;

            if (!string.IsNullOrEmpty(helpMessage))
            {
                builder.Append(helpMessage).AppendLine();
            }

            builder.AppendLine("[/info]");

            return builder.ToString();
        }
    }
}
