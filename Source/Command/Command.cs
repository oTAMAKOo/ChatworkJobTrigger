
using System.Linq;
using System.Text;

namespace ChatworkJobTrigger
{
    public sealed class Command
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        public string CommandName { get; private set; }

        public CommandArgument[] Arguments { get; private set; }

        public string JobNameFormat { get; private set; }

        //----- method -----

        public Command(string jobNameFormat, string commandName, CommandArgument[] arguments)
        {
            CommandName = commandName;
            Arguments = arguments;
            JobNameFormat = jobNameFormat;
        }

        public string GetHelpText()
        {
            var builder = new StringBuilder();

            builder.AppendFormat("[info][title]{0} Help[/title]", CommandName);
            
            var commandStr = $"Format : {CommandName}";

            foreach (var argument in Arguments)
            {
                commandStr += $" [{argument.Field.ToLower()}]";
            }
            
            builder.AppendLine(commandStr);
            builder.AppendLine();

            foreach (var argument in Arguments)
            {
                // タイトル.

                var titleStr = $"[{argument.Field.ToLower()}]";

                if (!string.IsNullOrEmpty(argument.Description))
                {
                    titleStr += $" {argument.Description}";
                }

                titleStr += argument.Require ? string.Empty : " (Option)";

                builder.AppendLine(titleStr);

                // 値候補.

                var valueStr = string.Empty;

                if (argument.ValuePattern.Any())
                {
                    foreach (var valuePattern in argument.ValuePattern)
                    {
                        if (!string.IsNullOrEmpty(valueStr))
                        {
                            valueStr += ", ";
                        }

                        valueStr += valuePattern.Key;
                        
                        if (valuePattern.Value.Any())
                        {
                            valueStr += $"[{string.Join(", ", valuePattern.Value)}]";
                        }
                    }

                    builder.AppendLine($"Values = {valueStr}");
                }

                // デフォルト値.

                if (!string.IsNullOrEmpty(argument.DefaultValue))
                {
                    builder.AppendLine($"Default = {argument.DefaultValue}");
                }

                builder.AppendLine();
            }

            builder.AppendLine("[/info]");

            return builder.ToString();
        }
    }
}
