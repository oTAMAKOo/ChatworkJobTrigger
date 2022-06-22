
using System.IO;
using System.Reflection;

namespace ChatworkJobTrigger
{
    public static class AssemblyUtility
    {
        public static string GetName()
        {
            return Assembly.GetExecutingAssembly().GetName().Name;
        }

        public static string GetExecutePath()
        {
            var assembly = Assembly.GetEntryAssembly();
            
            return Path.GetDirectoryName(assembly.Location);
        }
    }
}
