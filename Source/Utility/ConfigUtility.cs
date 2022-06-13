using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;

namespace ChatworkJenkinsBot
{
    public static class ConfigUtility
    {
        public static string GetConfigFolderDirectory()
        {
            var configFileDirectory = PathUtility.Combine(Directory.GetCurrentDirectory(), Constants.ConfigFolderName);

            if (!Directory.Exists(configFileDirectory))
            {
                Directory.CreateDirectory(configFileDirectory);
            }

            return configFileDirectory;
        }
    }
}
