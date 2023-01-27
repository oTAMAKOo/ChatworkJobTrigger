
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Extensions;
using IniFileParser.Model;

namespace ChatworkJobTrigger
{
    public sealed class CommandFileLoader : IniFile<CommandFileLoader>
    {
        //----- params -----

        private const string CommandFolderName = "command";

        private const string JobSection = "Job";

        private const string JobNameFormatKeyName = "JobNameFormat";

        private const string ArgumentSectionFormat = "Arg{0}";
        
        private const string FieldKeyName = "Field";
        private const string TypeKeyName = "Type";
        private const string ValueKeyName = "Values";
        private const string DefaultKeyName = "Default";
        private const string DescriptionKeyName = "Description";

        //----- field -----

        private string fileName = null;
        
        //----- property -----

        public override string FileName { get { return fileName; } }

        //----- method -----

        public async Task<Command> Load(string commandName)
        {
            fileName = PathUtility.Combine(CommandFolderName, commandName);

            await Load();

            var jobNameFormat = string.Empty;
            
            if(iniData.Sections.Any(x => x.SectionName == JobSection))
            {
                jobNameFormat = iniData[JobSection][JobNameFormatKeyName];
            }

            var arguments = new List<CommandArgument>();

            var index = 0;

            while (true)
            {
                var sectionName = string.Format(ArgumentSectionFormat, index);

                if (iniData.Sections.ContainsSection(sectionName))
                {
                    var fieldName = GetData<string>(sectionName, FieldKeyName);
                    var typeName = GetData<string>(sectionName, TypeKeyName);
                    var values = GetData<string>(sectionName, ValueKeyName);
                    var defaultValue = GetData<string>(sectionName, DefaultKeyName);
                    var description = GetData<string>(sectionName, DescriptionKeyName);

                    var argument = new CommandArgument(fieldName, typeName, values, defaultValue, description);

                    arguments.Add(argument);
                }
                else
                {
                    break;
                }

                index++;
            }

            var command = new Command(jobNameFormat, commandName, arguments.ToArray());

            return command;
        }

        private new async Task Load()
        {
            await base.Load();
        }
    }
}
