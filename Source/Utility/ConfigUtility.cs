
using System.IO;
using System.Reflection;
using Extensions;

namespace ChatworkJobTrigger
{
    public static class ConfigUtility
    {
        public const string ConfigFolderName = "config";

        public static string GetConfigFolderDirectory()
        {
            var executePath = AssemblyUtility.GetExecutePath();
            var configFileDirectory = PathUtility.Combine(executePath, ConfigFolderName);

            if (!Directory.Exists(configFileDirectory))
            {
                Directory.CreateDirectory(configFileDirectory);
            }

            return configFileDirectory;
        }
    }
}
