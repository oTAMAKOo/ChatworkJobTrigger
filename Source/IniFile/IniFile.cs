
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using IniFileParser;
using IniFileParser.Model;

namespace ChatworkJobTrigger
{
    public abstract class IniFile<TInstance> : Singleton<TInstance> where TInstance : IniFile<TInstance>
    {
        //----- params -----

        //----- field -----

        protected IniData iniData = null;

        //----- property -----

        public abstract string FileName { get; }

        //----- method -----

        public async Task Load()
        {
            var fileName = FileName + ".ini";

            var configFileDirectory = ConfigUtility.GetConfigFolderDirectory();

            var configFilePath = PathUtility.Combine(configFileDirectory, fileName);

            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"{configFilePath} not found.");
            }

            var iniDataString = await File.ReadAllTextAsync(configFilePath);

            var parser = new IniStringParser();

            iniData = parser.Parse(iniDataString);

            OnLoad();
        }

        protected T GetData<T>(string section, string key, T defaultValue = default)
        {
            var value = iniData[section][key];

            if (!iniData[section].ContainsKey(key))
            {
                return defaultValue;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }

        protected virtual void OnLoad(){  }
    }
}
