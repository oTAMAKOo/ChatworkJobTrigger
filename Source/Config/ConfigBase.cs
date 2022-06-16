
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using IniFileParser;
using IniFileParser.Model;

namespace ChatworkJenkinsBot
{
    public abstract class ConfigBase<TInstance> : Singleton<TInstance> where TInstance : ConfigBase<TInstance>
    {
        //----- params -----

        //----- field -----

        protected IniData iniData = null;

        //----- property -----

        public abstract string ConfigIniName { get; }

        //----- method -----

        public async Task Load()
        {
            var parser = new IniStringParser();

            var configFileDirectory = ConfigUtility.GetConfigFolderDirectory();

            var configFilePath = PathUtility.Combine(configFileDirectory, ConfigIniName);

            if (!File.Exists(configFilePath))
            {
                CreateDefaultConfigFile(configFilePath);

                throw new Exception($"Generated {ConfigIniName} file.\nPlease edit {ConfigIniName}.");
            }

            var iniDataString = await File.ReadAllTextAsync(configFilePath);

            iniData = parser.Parse(iniDataString);

            OnLoad();
        }

        private void CreateDefaultConfigFile(string configFilePath)
        {
            var parser = new IniFileParser.IniFileParser();

            var data = new IniData();

            SetDefaultData(ref data);

            parser.WriteFile(configFilePath, data, Encoding.UTF8);
        }

        protected T GetData<T>(string section, string key)
        {
            var value = iniData[section][key];

            return (T)Convert.ChangeType(value, typeof(T));
        }

        protected abstract void SetDefaultData(ref IniData iniData);

        protected virtual void OnLoad(){  }
    }
}
