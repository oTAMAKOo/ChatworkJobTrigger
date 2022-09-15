
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatworkJobTrigger
{
    public sealed class CommandArgument
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        /// <summary> フィールド名 </summary>
        public string Field { get; private set; }

        /// <summary> 型 </summary>
        public Type Type { get; private set; }

        /// <summary> デフォルト値 (空の場合は必須扱い) </summary>
        public string DefaultValue { get; private set; }

        /// <summary> 必須か </summary>
        public bool Require { get; private set; }

        /// <summary> 説明 </summary>
        public string Description { get; private set; }

        /// <summary> 値:パターンの辞書 </summary>
        public Dictionary<string, string[]> ValuePattern { get; private set; }

        //----- method -----

        public CommandArgument(string fieldName, string typeName, string values, string defaultValue, string description)
        {
            Field = fieldName.Trim();
            Type = GetType(typeName);
            DefaultValue = defaultValue != null ? defaultValue.Trim() : null;
            Require = string.IsNullOrEmpty(DefaultValue);
            Description = description;

            ValuePattern = ParsePatternStr(values);
        }

        private Type GetType(string typeName)
        {
            var textDefine = TextDefine.Instance;

            Type type = null;

            switch (typeName)
            {
                case "string":
                    type = typeof(string);
                    break;
                case "int":
                    type = typeof(int);
                    break;
                case "bool":
                    type = typeof(bool);
                    break;
                default:
                    {
                        var errorMessage = textDefine.UndefinedTypeError.Replace("#TYPE_NAME#", typeName);

                        throw new ArgumentException(errorMessage);
                    }
            }

            return type;
        }

        public string ConvertValue(string value)
        {
            if (!ValuePattern.Any()) { return value; }

            if(ValuePattern.ContainsKey(value)){ return value; }

            foreach (var pattern in ValuePattern)
            {
                foreach (var item in pattern.Value)
                {
                    if (item == value){ return pattern.Key; }
                }
            } 
            
            return null;
        }

        private Dictionary<string, string[]> ParsePatternStr(string target)
        {
            var dictionary = new Dictionary<string, string[]>();

            if (string.IsNullOrEmpty(target)){ return dictionary; }

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
    }
}
