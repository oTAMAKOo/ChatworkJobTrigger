
using System.IO;
using Extensions;

namespace ChatworkJenkinsBot
{
    public static class ConfigUtility
    {
        public const string ConfigFolderName = "config";

        public static string GetConfigFolderDirectory()
        {
            var configFileDirectory = PathUtility.Combine(Directory.GetCurrentDirectory(), ConfigFolderName);

            if (!Directory.Exists(configFileDirectory))
            {
                Directory.CreateDirectory(configFileDirectory);
            }

            return configFileDirectory;
        }
    }
}
