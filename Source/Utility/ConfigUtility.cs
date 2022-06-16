
using System.IO;
using System.Reflection;
using Extensions;

namespace ChatworkJenkinsBot
{
    public static class ConfigUtility
    {
        public const string ConfigFolderName = "config";

        private static string GetExecutePath()
        {
            var assembly = Assembly.GetEntryAssembly();
            
            return Path.GetDirectoryName(assembly.Location);
        }

        public static string GetConfigFolderDirectory()
        {
            var configFileDirectory = PathUtility.Combine(GetExecutePath(), ConfigFolderName);

            if (!Directory.Exists(configFileDirectory))
            {
                Directory.CreateDirectory(configFileDirectory);
            }

            return configFileDirectory;
        }
    }
}
